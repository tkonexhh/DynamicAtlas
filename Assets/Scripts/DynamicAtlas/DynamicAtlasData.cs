using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GFrame
{


    public class SaveTextureData
    {
        public int texIndex = -1;
        public int referenceCount = 0;
        public Rect rect;
    }

    public class GetTextureData
    {
        public string name;
        public OnCallBackTexRect callback;
    }

    public class IntegerRectangle
    {
        public int x;
        public int y;
        public int width;
        public int height;

        public int right => x + width;
        public int top => y + height;
        public int size => width * height;

        public Rect rect => new Rect(x, y, width, height);

        public IntegerRectangle(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public override string ToString()
        {
            return string.Format("x{0}_y:{1}_width:{2}_height{3}_top:{4}_right{5}", x, y, width, height, top, right);
        }
    }
}