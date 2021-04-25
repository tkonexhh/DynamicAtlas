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
        // static public readonly string VERSION = "1.3.0";
        private int mWidth, mHeight = 0;
        private int mPadding = 8;
        private int mPackedWidth, mPackedHeight = 0;

        private List<SortableSize> mInsertList = new List<SortableSize>();

        private List<IntegerRectangle> mInsertedRectangles = new List<IntegerRectangle>();
        private List<IntegerRectangle> mFreeAreas = new List<IntegerRectangle>();
        private List<IntegerRectangle> mNewFreeAreas = new List<IntegerRectangle>();

        private IntegerRectangle mOutsideRectangle;

        public int rectangleCount { get { return mInsertedRectangles.Count; } }

        // public int packedWidth { get { return mPackedWidth; } }
        // public int packedHeight { get { return mPackedHeight; } }
        // public int padding { get { return mPadding; } }

        public RectanglePacker(int width, int height, int padding = 0)
        {
            mOutsideRectangle = RectanglePackerMgr.S.AllocateIntegerRectangle(width + 1, height + 1, 0, 0);
            reset(width, height, padding);
        }

        public void reset(int width, int height, int padding = 0)
        {
            while (mInsertedRectangles.Count > 0)
                RectanglePackerMgr.S.ReleaseIntegerRectangle(mInsertedRectangles.Pop());

            while (mFreeAreas.Count > 0)
                RectanglePackerMgr.S.ReleaseIntegerRectangle(mFreeAreas.Pop());

            mWidth = width;
            mHeight = height;

            mPackedWidth = 0;
            mPackedHeight = 0;

            mFreeAreas.Add(RectanglePackerMgr.S.AllocateIntegerRectangle(0, 0, mWidth, mHeight));//默认添加一个铺满的区域

            while (mInsertList.Count > 0)
                RectanglePackerMgr.S.ReleaseSize(mInsertList.Pop());

            mPadding = padding;
        }

        public IntegerRectangle getRectangle(int index, IntegerRectangle rectangle)
        {

            IntegerRectangle inserted = mInsertedRectangles[index];

            rectangle.x = inserted.x;
            rectangle.y = inserted.y;
            rectangle.width = inserted.width;
            rectangle.height = inserted.height;

            return rectangle;
        }

        public int getRectangleId(int index)
        {
            IntegerRectangle inserted = mInsertedRectangles[index];
            return inserted.id;
        }

        public void insertRectangle(int width, int height, int id)
        {
            SortableSize sortableSize = RectanglePackerMgr.S.AllocateSize(width, height, id);
            mInsertList.Add(sortableSize);
        }

        public int packRectangles(bool sort = true)
        {
            if (sort)
                mInsertList.Sort((emp1, emp2) => emp1.width.CompareTo(emp2.width));

            while (mInsertList.Count > 0)
            {

                SortableSize sortableSize = mInsertList.Pop();
                int width = sortableSize.width;
                int height = sortableSize.height;

                int index = getFreeAreaIndex(width, height);
                if (index >= 0)
                {

                    IntegerRectangle freeArea = mFreeAreas[index];
                    IntegerRectangle target = RectanglePackerMgr.S.AllocateIntegerRectangle(freeArea.x, freeArea.y, width, height);
                    target.id = sortableSize.id;

                    // Generate the new free areas, these are parts of the old ones intersected or touched by the target
                    generateNewFreeAreas(target, mNewFreeAreas);

                    while (mNewFreeAreas.Count > 0)
                        mFreeAreas.Add(mNewFreeAreas.Pop());

                    mInsertedRectangles.Add(target);

                    if (target.right > mPackedWidth)
                        mPackedWidth = target.right;

                    if (target.bottom > mPackedHeight)
                        mPackedHeight = target.bottom;
                }

                RectanglePackerMgr.S.ReleaseSize(sortableSize);
            }

            return rectangleCount;
        }

        private void generateNewFreeAreas(IntegerRectangle target, List<IntegerRectangle> results)
        {
            // Increase dimensions by one to get the areas on right / bottom this rectangle touches
            // Also add the padding here
            int x = target.x;
            int y = target.y;
            int right = target.right + 1 + mPadding;
            int bottom = target.bottom + 1 + mPadding;

            IntegerRectangle targetWithPadding = null;
            if (mPadding == 0)
                targetWithPadding = target;

            for (int i = mFreeAreas.Count - 1; i >= 0; i--)
            {
                IntegerRectangle area = mFreeAreas[i];
                if (!(x >= area.right || right <= area.x || y >= area.bottom || bottom <= area.y))
                {

                    if (targetWithPadding == null)
                        targetWithPadding = RectanglePackerMgr.S.AllocateIntegerRectangle(target.x, target.y, target.width + mPadding, target.height + mPadding);

                    generateDividedAreas(targetWithPadding, area, results);
                    IntegerRectangle topOfStack = mFreeAreas.Pop();
                    if (i < mFreeAreas.Count)
                    {

                        // Move the one on the top to the freed position
                        mFreeAreas[i] = topOfStack;
                    }
                }
            }

            if (targetWithPadding != null && targetWithPadding != target)
                RectanglePackerMgr.S.ReleaseIntegerRectangle(targetWithPadding);

            filterSelfSubAreas(results);
        }

        private void filterSelfSubAreas(List<IntegerRectangle> areas)
        {

            for (int i = areas.Count - 1; i >= 0; i--)
            {

                IntegerRectangle filtered = areas[i];
                for (int j = areas.Count - 1; j >= 0; j--)
                {

                    if (i != j)
                    {

                        IntegerRectangle area = areas[j];
                        if (filtered.x >= area.x && filtered.y >= area.y && filtered.right <= area.right && filtered.bottom <= area.bottom)
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

        private void generateDividedAreas(IntegerRectangle divider, IntegerRectangle area, List<IntegerRectangle> results)
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

            int bottomDelta = area.bottom - divider.bottom;
            if (bottomDelta > 0)
            {
                results.Add(RectanglePackerMgr.S.AllocateIntegerRectangle(area.x, divider.bottom, area.width, bottomDelta));
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

        private int getFreeAreaIndex(int width, int height)
        {
            IntegerRectangle best = mOutsideRectangle;
            int index = -1;

            int paddedWidth = width + mPadding;
            int paddedHeight = height + mPadding;

            int count = mFreeAreas.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                IntegerRectangle free = mFreeAreas[i];
                if (free.x < mPackedWidth || free.y < mPackedHeight)
                {
                    // Within the packed area, padding required
                    if (free.x < best.x && paddedWidth <= free.width && paddedHeight <= free.height)
                    {

                        index = i;
                        if ((paddedWidth == free.width && free.width <= free.height && free.right < mWidth) || (paddedHeight == free.height && free.height <= free.width))
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
                        if ((width == free.width && free.width <= free.height && free.right < mWidth) || (height == free.height && free.height <= free.width))
                            break;

                        best = free;
                    }
                }
            }

            return index;
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