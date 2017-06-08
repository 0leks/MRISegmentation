using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RegionGrowingJob : ThreadedJob {

    private ImageSegmentationHandler2 m_segmentationHandler;
    private DataContainer m_data;
    private float m_threshold;

    private bool[,,] visited;

    public RegionGrowingJob(ImageSegmentationHandler2 segmentationHandler, DataContainer data, float threshold) {
        m_segmentationHandler = segmentationHandler;
        m_data = data;
        m_threshold = threshold;
    }

    // this runs in the new thread
    protected override void ThreadFunction() {
        Debug.LogError( "beginning flood fill segmenting" );
        visited = DoFloodFillSegmentation();
        //int count = 0;
        //for( int a = 0; a < visited.GetLength( 2 ); a++ ) {
        //    for( int b = 0; b < visited.GetLength( 0 ); b++ ) {
        //        string row = "";
        //        for( int c = 0; c < visited.GetLength( 1 ); c++ ) {
        //            if( visited[a, b, c] ) {
        //                row += "#";
        //            }
        //            else {
        //                row += "-";
        //            }
        //        }
        //        Debug.Log( row  + count ++);
        //    }
        //    Debug.Log( "~~~" + count++ );
        //}
        Debug.LogError( "finished flood fill segmenting" );
    }

    // This runs in the main thread
    protected override void OnFinished() {
        Debug.LogError( "entered OnFinished for flood fill" );
        m_data.AddSegment( visited );
        m_segmentationHandler.FinishedSegmentationCallback();
    }

    public bool[,,] DoFloodFillSegmentation() {
        bool[,,] visited = new bool[ m_data.getWidth(), m_data.getHeight(), m_data.getNumLayers() ];
        Stack<DataContainer.Point> searchArea = new Stack<DataContainer.Point>();
        Stack<float> searchAreaFrom = new Stack<float>();
        foreach( DataContainer.Point obj in m_data.GetObjectSeeds() ) {
            searchArea.Push( new DataContainer.Point( obj.x, obj.y, obj.z ) );
            searchAreaFrom.Push( m_data.getOriginalPixelFloat( obj.x, obj.y, obj.z ) );
        }
        while( searchArea.Count > 0 ) {
            DataContainer.Point point = searchArea.Pop();
            float from = searchAreaFrom.Pop();

            if( point.x >= 0 && point.x < m_data.getWidth() && point.y >= 0 && point.y < m_data.getHeight() && point.z >= 0 && point.z < m_data.getNumLayers() ) {
                if( !visited[ point.x, point.y, point.z ] ) {
                    float color = m_data.getOriginalPixelFloat( point.x, point.y, point.z );
                    float diff = Mathf.Abs( from - color );
                    if( diff <= m_threshold ) {
                        visited[ point.x, point.y, point.z ] = true;
                        searchArea.Push( new DataContainer.Point( point.x - 1, point.y, point.z ) );
                        searchArea.Push( new DataContainer.Point( point.x + 1, point.y, point.z ) );
                        searchArea.Push( new DataContainer.Point( point.x, point.y - 1, point.z ) );
                        searchArea.Push( new DataContainer.Point( point.x, point.y + 1, point.z ) );
                        searchArea.Push( new DataContainer.Point( point.x, point.y, point.z + 1 ) );
                        searchArea.Push( new DataContainer.Point( point.x, point.y, point.z - 1 ) );
                        searchAreaFrom.Push( color );
                        searchAreaFrom.Push( color );
                        searchAreaFrom.Push( color );
                        searchAreaFrom.Push( color );
                        searchAreaFrom.Push( color );
                        searchAreaFrom.Push( color );
                    }
                }
            }
        }
        //saveSegmentToFile(visited, originalScans, "Resources/segment/segment");
        //m_data.saveSegmentToFileAsImages( visited, "Resources/segment/" + segmentName );
        return visited;
    }
}
