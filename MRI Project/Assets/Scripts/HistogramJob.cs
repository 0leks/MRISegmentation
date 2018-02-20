using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class HistogramJob : ThreadedJob {

    private ImageSegmentationHandler2 m_segmentationHandler;
    private DataContainer m_data;
    
    private int[] histogram;
    private int[] sums;
    private int[] groupLabels;
    private int totalLabels;
    private List<int[]> histogramGroups;
    private List<bool[,,]> segments;

    private double minimumRatio = 0.1;
    private int totalNonZero;
    private int groupWidth = 20;
    private int minimumBrightness = 30;

    public HistogramJob( ImageSegmentationHandler2 handler, DataContainer data) {
        m_segmentationHandler = handler;
        m_data = data;
    }

    // this runs in the new thread
    protected override void ThreadFunction() {
        Debug.LogError( "beginning histogram" );
        histogram = ComputeHistogram();
        FindHistogramGroups();
        GroupLabels();

        WriteToFile();
        Debug.LogError( "finished writing to file" );

        segments = GroupSegmentation();

        Debug.LogError( "finished histogram" );
    }

    private void GroupLabels() {
        groupLabels = new int[ 256 ];
        for( int i = 0; i < histogramGroups.Count; i++ ) {
            int groupStart = histogramGroups[ i ][ 0 ];
            int groupEnd = groupStart + histogramGroups[ i ][ 1 ];
            for( int w = groupStart; w < groupEnd; w++ ) {
                groupLabels[ w ] = i + 1;
            }
        }
        int label = histogramGroups.Count + 1;
        bool used = false;
        for( int i = minimumBrightness; i < groupLabels.Length; i++ ) {
            if( groupLabels[i] == 0 ) {
                groupLabels[ i ] = label;
                used = true;
                totalLabels = label;
            }
            else if( used ) {
                label++;
                used = false;
            }
        }
        Debug.LogError("Total " + totalLabels + " group labels" );
    }
    private List<bool[,,]> GroupSegmentation() {

        List<bool[,,]> segments = new List<bool[,,]>();
        for( int i = 0; i <= totalLabels; i++ ) {
            bool[,,] visited = new bool[ m_data.getWidth(), m_data.getHeight(), m_data.getNumLayers() ];
            segments.Add( visited );
        }
        byte[,,] byteData = m_data.getByteData();
        for( int x = 0; x < byteData.GetLength( 0 ); x++ ) {
            //Debug.Log( "x=" + x );
            for( int y = 0; y < byteData.GetLength( 1 ); y++ ) {
                //Debug.Log( x + "," +  "y=" + y );
                for( int z = 0; z < byteData.GetLength( 2 ); z++ ) {
                    //Debug.Log( x + "," + y + "," + "z=" + z + " data " + byteData[ x, y, z ] + " label " + groupLabels[ byteData[ x, y, z ] ] );
                    segments[ groupLabels[ byteData[ x, y, z ] ] ][ x, y, z ] = true;
                }
            }
        }
        segments.RemoveAt( 0 );
        return segments;
    }
    private void FindHistogramGroups() {
        int[] sums = new int[ histogram.Length - groupWidth ];
        int[] sums2 = new int[ histogram.Length - groupWidth ];
        for( int i = minimumBrightness; i < sums.Length; i++ ) {
            for( int w = 0; w < groupWidth; w++ ) {
                sums[ i ] += histogram[ i + w ];
                sums2[ i ] += histogram[ i + w ];
            }
        }
        this.sums = sums;
        sums2[ 0 ] = 0;
        histogramGroups = new List<int[]>();
        int minimumAmount = (int) ( totalNonZero * minimumRatio);
        Debug.LogError( "Using " + minimumAmount + " minimum amount per group");
        while( true ) {
            int maximumIndex = 0;
            for( int i = 1; i < sums2.Length; i++ ) {
                if( sums2[ i ] > sums2[ maximumIndex ] ) {
                    maximumIndex = i;
                }
            }
            if( sums[ maximumIndex ] >= minimumAmount ) {
                histogramGroups.Add( new int[] { maximumIndex, groupWidth } );
                Debug.LogError( "Added group " + maximumIndex + ", " + groupWidth );
                for( int i = maximumIndex - groupWidth + 1; i < maximumIndex + groupWidth; i++ ) {
                    if( i >= 0 && i < sums2.Length ) {
                        sums2[ i ] = 0;
                    }
                }
            }
            else {
                break;
            }
        }
    }

    private void WriteToFile() {
        string path = "Assets/Resources/histogram.txt";
        //Write some text to the test.txt file
        StreamWriter writer = new StreamWriter( path, false );
        string text = "";
        for( int i = 0; i < histogram.Length; i++ ) {
            text += histogram[ i ] + "\t";
        }
        writer.WriteLine( text );
        text = "";
        for( int i = 0; i < sums.Length; i++ ) {
            text += sums[ i ] + "\t";
        }
        writer.WriteLine( text );
        text = "";
        for( int i = 0; i < groupLabels.Length; i++ ) {
            text += groupLabels[ i ] + "\t";
        }
        writer.WriteLine( text );
        text = "";
        for( int i = 0; i < histogramGroups.Count; i++ ) {
            text += histogramGroups[ i ][ 0 ] + "\t";
        }
        writer.WriteLine( text );
        writer.Close();
    }

    public int[] ComputeHistogram() {
        int[] histogram = new int[256];
        byte[,,] byteData = m_data.getByteData();
        for( int x = 0; x < byteData.GetLength(0); x++ ) {
            for( int y = 0; y < byteData.GetLength(1); y++ ) {
                for( int z = 0; z < byteData.GetLength( 2 ); z++ ) {
                    histogram[ byteData[ x, y, z ] ]++;
                }
            }
        }
        totalNonZero = m_data.getWidth() * m_data.getHeight() * m_data.getNumLayers() - histogram[ 0 ];
        return histogram;
    }

    // This runs in the main thread
    protected override void OnFinished() {
        Debug.LogError( "entered OnFinished for histogram" );
        for( int i = 0; i < segments.Count; i++ ) {
            Debug.LogError( "Adding segment " + i );
            m_data.AddSegment( segments[ i ] );
        }
        m_segmentationHandler.HistogramFinished(this);
    }

    
}
