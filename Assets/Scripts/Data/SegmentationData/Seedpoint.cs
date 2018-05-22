using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SegmentationData
{
    public struct Seedpoint
    {
        int x, y, z;

        public Seedpoint (int _x, int _y, int _z)
        {
            x = _x;
            y = _y;
            z = _z;
        }

        public Seedpoint (float _x, float _y, float _z)
        {
            x = (int)_x;
            y = (int)_y;
            z = (int)_z;
        }

        private Vector3 CubeToSeedCoordinates (Vector3 cubeCoords)
        {
            // cube coordinates will be [-0.5, 0.5]
            return Vector3.zero;   
        }
    }
}
