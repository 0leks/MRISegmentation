using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SegmentationData
{
    public class SeedPoints
    {
        private List<Vector3Int> m_ForegroundSeedPoints;
        private List<Vector3Int> m_BackgroundSeedPoints;

        private int DataWidth;
        private int DataHeight;
        private int DataDepth;

        public SeedPoints (int dataWidth, int dataHeight, int dataDepth)
        {
            m_ForegroundSeedPoints = new List<Vector3Int>();
            m_BackgroundSeedPoints = new List<Vector3Int>();
            DataWidth = dataWidth;
            DataHeight = dataHeight;
            DataDepth = dataDepth;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="localPos">Location of the new seed point in cube coordinates</param>
        /// <param name="foreground">True if the seed is a part of the foreground, false part of the background</param>
        public void AddSeedPoint(Vector3 localPos, bool foreground)
        {
            Vector3Int seed = CubeToScanPixelCoordinates(localPos);

            if (foreground)
            {
                m_ForegroundSeedPoints.Add(seed);
            }
            else
            {
                m_BackgroundSeedPoints.Add(seed);
            }
        }

        private Vector3Int CubeToScanPixelCoordinates(Vector3 localPos)
        {
            int xCoord = (int)((0.5f - localPos.x) * DataWidth);
            int yCoord = (int)((0.5f - localPos.z) * DataHeight);
            int zCoord = (int)((localPos.y + 0.5f) * DataDepth);

            return (new Vector3Int(xCoord, yCoord, zCoord));
        }

        public List<Vector3Int> GetForeground()
        {
            return m_ForegroundSeedPoints;
        }

        public List<Vector3Int> GetBackground()
        {
            return m_BackgroundSeedPoints;
        }
    }
}
