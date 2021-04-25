/**
 * Rectangle packer
 *
 * Copyright 2012 Ville Koskela. All rights reserved.
 * Ported to Unity by Da Viking Code.
 *
 * Email: ville@villekoskela.org
 * Blog: http://villekoskela.org
 * Twitter: @villekoskelaorg
 *
 * You may redistribute, use and/or modify this source code freely
 * but this copyright statement must not be removed from the source files.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
 * ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE. *
 *
 */
using System.Collections.Generic;

namespace DaVikingCode.RectanglePacking
{

    /**
     * Class used to pack rectangles within container rectangle with close to optimal solution.
     */
    public class RectanglePacker
    {
        private int m_Width, m_Height = 0;
        private int m_Padding = 8;
        private int m_PackedWidth, m_PackedHeight = 0;

        private List<SortableSize> m_InsertList = new List<SortableSize>();

        private List<IntegerRectangle> m_InsertedRectangles = new List<IntegerRectangle>();
        private List<IntegerRectangle> m_FreeAreas = new List<IntegerRectangle>();
        private List<IntegerRectangle> m_TempFreeAreas = new List<IntegerRectangle>();

        private IntegerRectangle mOutsideRectangle;

        public int rectangleCount { get { return m_InsertedRectangles.Count; } }

        // public int packedWidth { get { return mPackedWidth; } }
        // public int packedHeight { get { return mPackedHeight; } }
        // public int padding { get { return mPadding; } }

        public RectanglePacker(int width, int height, int padding = 0)
        {
            mOutsideRectangle = RectanglePackerMgr.S.AllocateIntegerRectangle(width + 1, height + 1, 0, 0);
            Reset(width, height, padding);
        }

        public void Reset(int width, int height, int padding = 0)
        {
            while (m_InsertedRectangles.Count > 0)
                RectanglePackerMgr.S.ReleaseIntegerRectangle(m_InsertedRectangles.Pop());

            while (m_FreeAreas.Count > 0)
                RectanglePackerMgr.S.ReleaseIntegerRectangle(m_FreeAreas.Pop());

            m_Width = width;
            m_Height = height;

            m_PackedWidth = 0;
            m_PackedHeight = 0;

            m_FreeAreas.Add(RectanglePackerMgr.S.AllocateIntegerRectangle(0, 0, m_Width, m_Height));//默认添加一个铺满的区域

            while (m_InsertList.Count > 0)
                RectanglePackerMgr.S.ReleaseSize(m_InsertList.Pop());

            m_Padding = padding;
        }

        public IntegerRectangle GetRectangle(int index, IntegerRectangle rectangle)
        {
            IntegerRectangle inserted = m_InsertedRectangles[index];
            rectangle.x = inserted.x;
            rectangle.y = inserted.y;
            rectangle.width = inserted.width;
            rectangle.height = inserted.height;
            return rectangle;
        }

        public int GetRectangleId(int index)
        {
            IntegerRectangle inserted = m_InsertedRectangles[index];
            return inserted.id;
        }

        public void InsertRectangle(int width, int height, int id)
        {
            SortableSize sortableSize = RectanglePackerMgr.S.AllocateSize(width, height, id);
            m_InsertList.Add(sortableSize);
        }

        private int GetFreeAreaIndex(int width, int height)
        {
            IntegerRectangle best = mOutsideRectangle;
            int index = -1;

            int paddedWidth = width + m_Padding;
            int paddedHeight = height + m_Padding;

            for (int i = m_FreeAreas.Count - 1; i >= 0; i--)
            {
                IntegerRectangle free = m_FreeAreas[i];
                if (free.x < m_PackedWidth || free.y < m_PackedHeight)
                {
                    // Within the packed area, padding required
                    if (free.x < best.x && paddedWidth <= free.width && paddedHeight <= free.height)
                    {
                        index = i;
                        if ((paddedWidth == free.width && free.width <= free.height && free.right < m_Width) || (paddedHeight == free.height && free.height <= free.width))
                            break;

                        best = free;
                    }

                }
                else
                {
                    // Outside the current packed area, no padding required
                    if (free.x < best.x && width <= free.width && height <= free.height)
                    {
                        index = i;
                        if ((width == free.width && free.width <= free.height && free.right < m_Width) || (height == free.height && free.height <= free.width))
                            break;

                        best = free;
                    }
                }
            }

            return index;
        }

        public int PackRectangles(bool sort = true)
        {
            if (sort)
                m_InsertList.Sort((emp1, emp2) => emp1.width.CompareTo(emp2.width));

            while (m_InsertList.Count > 0)
            {
                SortableSize sortableSize = m_InsertList.Pop();
                int width = sortableSize.width;
                int height = sortableSize.height;

                int index = GetFreeAreaIndex(width, height);//根据W，H 得到下标
                if (index >= 0)
                {
                    IntegerRectangle freeArea = m_FreeAreas[index];//得到一个可以容纳target的Area
                    IntegerRectangle target = RectanglePackerMgr.S.AllocateIntegerRectangle(freeArea.x, freeArea.y, width, height);
                    target.id = sortableSize.id;

                    // Generate the new free areas, these are parts of the old ones intersected or touched by the target
                    GenerateNewFreeAreas(target, m_TempFreeAreas);

                    while (m_TempFreeAreas.Count > 0)
                        m_FreeAreas.Add(m_TempFreeAreas.Pop());

                    m_InsertedRectangles.Add(target);

                    if (target.right > m_PackedWidth)
                        m_PackedWidth = target.right;

                    if (target.top > m_PackedHeight)
                        m_PackedHeight = target.top;
                }

                RectanglePackerMgr.S.ReleaseSize(sortableSize);
            }

            return rectangleCount;
        }

        private void GenerateNewFreeAreas(IntegerRectangle target, List<IntegerRectangle> results)
        {
            // Increase dimensions by one to get the areas on right / bottom this rectangle touches
            // Also add the padding here
            int x = target.x;
            int y = target.y;
            int right = target.right + 1 + m_Padding;
            int top = target.top + 1 + m_Padding;

            IntegerRectangle targetWithPadding = null;
            if (m_Padding == 0)
                targetWithPadding = target;

            for (int i = m_FreeAreas.Count - 1; i >= 0; i--)
            {
                IntegerRectangle area = m_FreeAreas[i];
                if (!(x >= area.right || right <= area.x || y >= area.top || top <= area.y))
                {
                    UnityEngine.Debug.LogError(target.x + ":" + area.x);
                    if (targetWithPadding == null)
                        targetWithPadding = RectanglePackerMgr.S.AllocateIntegerRectangle(target.x, target.y, target.width + m_Padding, target.height + m_Padding);

                    GenerateDividedAreas(targetWithPadding, area, results);
                    IntegerRectangle topOfStack = m_FreeAreas.Pop();
                    if (i < m_FreeAreas.Count)
                    {
                        // Move the one on the top to the freed position
                        m_FreeAreas[i] = topOfStack;
                    }
                }
            }

            if (targetWithPadding != null && targetWithPadding != target)
                RectanglePackerMgr.S.ReleaseIntegerRectangle(targetWithPadding);

            FilterSelfSubAreas(results);
        }

        private void GenerateDividedAreas(IntegerRectangle divider, IntegerRectangle area, List<IntegerRectangle> results)
        {
            int count = 0;

            int rightDelta = area.right - divider.right;
            if (rightDelta > 0)
            {
                results.Add(RectanglePackerMgr.S.AllocateIntegerRectangle(divider.right, area.y, rightDelta, area.height));
                count++;
            }

            int leftDelta = divider.x - area.x;
            if (leftDelta > 0)
            {
                results.Add(RectanglePackerMgr.S.AllocateIntegerRectangle(area.x, area.y, leftDelta, area.height));
                count++;
            }

            int bottomDelta = area.top - divider.top;
            if (bottomDelta > 0)
            {
                results.Add(RectanglePackerMgr.S.AllocateIntegerRectangle(area.x, divider.top, area.width, bottomDelta));
                count++;
            }

            int topDelta = divider.y - area.y;
            if (topDelta > 0)
            {
                results.Add(RectanglePackerMgr.S.AllocateIntegerRectangle(area.x, area.y, area.width, topDelta));
                count++;
            }

            if (count == 0 && (divider.width < area.width || divider.height < area.height))
            {
                // Only touching the area, store the area itself
                results.Add(area);

            }
            else
                RectanglePackerMgr.S.ReleaseIntegerRectangle(area);
        }

        private void FilterSelfSubAreas(List<IntegerRectangle> areas)
        {
            for (int i = areas.Count - 1; i >= 0; i--)
            {
                IntegerRectangle filtered = areas[i];
                for (int j = areas.Count - 1; j >= 0; j--)
                {
                    if (i != j)
                    {
                        IntegerRectangle area = areas[j];
                        if (filtered.x >= area.x && filtered.y >= area.y && filtered.right <= area.right && filtered.top <= area.top)
                        {
                            RectanglePackerMgr.S.ReleaseIntegerRectangle(filtered);
                            IntegerRectangle topOfStack = areas.Pop();
                            if (i < areas.Count)
                            {
                                // Move the one on the top to the freed position
                                areas[i] = topOfStack;
                            }
                            break;
                        }
                    }
                }
            }
        }


    }

    static class ListExtension
    {
        static public T Pop<T>(this List<T> list)
        {
            int index = list.Count - 1;

            T r = list[index];
            list.RemoveAt(index);
            return r;
        }

    }

    public class RectanglePackerMgr : Singleton<RectanglePackerMgr>
    {
        private List<IntegerRectangle> m_IntegerRectangleList = new List<IntegerRectangle>();
        private List<SortableSize> m_SortableSizeStack = new List<SortableSize>();

        public IntegerRectangle AllocateIntegerRectangle(int x, int y, int width, int height)
        {
            if (m_IntegerRectangleList.Count > 0)
            {
                IntegerRectangle rectangle = m_IntegerRectangleList.Pop();
                rectangle.x = x;
                rectangle.y = y;
                rectangle.width = width;
                rectangle.height = height;
            }
            return new IntegerRectangle(x, y, width, height);
        }

        public void ReleaseIntegerRectangle(IntegerRectangle rectangle)
        {
            m_IntegerRectangleList.Add(rectangle);
        }

        public SortableSize AllocateSize(int width, int height, int id)
        {
            if (m_SortableSizeStack.Count > 0)
            {
                SortableSize size = m_SortableSizeStack.Pop();
                size.width = width;
                size.height = height;
                size.id = id;
                return size;
            }

            return new SortableSize(width, height, id);
        }

        public void ReleaseSize(SortableSize size)
        {
            m_SortableSizeStack.Add(size);
        }
    }


}