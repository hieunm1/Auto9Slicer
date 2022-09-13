using System;
using UnityEngine;

namespace Auto9Slicer
{
    [Serializable]
    public class SliceOptions
    {
        public int Tolerate = 0;
        public int CenterSize = 2;
        public int Margin = 2;
        public int MinCropSize = 0;

        public UnityEngine.Object[] directories;
        public Texture2D[] textures;

        public static SliceOptions Default => new SliceOptions();
    }
}