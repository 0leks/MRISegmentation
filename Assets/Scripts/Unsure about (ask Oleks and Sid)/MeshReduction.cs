using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MeshReduction : MonoBehaviour {

	public void ReduceMesh( Vector3[] vertices, int[] triangles, List<Vector3> newVertices , List<int> newTriangles) {
        StreamWriter file = new StreamWriter( Application.dataPath + "/MeshReductionTest.txt" );
        Debug.Log( "Beginning mesh reduction" );
        Debug.Log( "Started with Number of vertices = " + vertices.Length );
        Debug.Log( "             Number of triangles = " + triangles.Length );

        if( triangles.Length % 3 != 0 ) {
            Debug.LogError( "ERROR! size of triangle array is not multiple of 3!!!!!!!");
        }
        int numTriangles = triangles.Length / 3;

        // Step 1. Each vertex needs a list of triangles that touch it.

        // Each vertex can have a max of 20 triangles touching it.
        int MAX_TRIANGLES = 100;
        int[] vertexTriangles = new int[ vertices.Length * MAX_TRIANGLES ];
        int[] tempIndices = new int[ vertices.Length ];
        for( int t = 0; t < numTriangles; t++ ) {
            for( int i = 0; i < 3; i ++ ) {
                int vertexNumber = triangles[ t * 3 + i ];
                if( tempIndices[ vertexNumber ] >= MAX_TRIANGLES ) {
                    Debug.LogError( "ERROR! too many triangles touching vertex " + vertexNumber );
                }
                vertexTriangles[ vertexNumber * MAX_TRIANGLES + tempIndices[ vertexNumber ] ] = t ;
                tempIndices[ vertexNumber ]++;
            }
        }

        int sum = 0;
        int max = -1;
        int min = 999999;
        for( int i = 0; i < tempIndices.Length; i++ ) {
            sum += tempIndices[ i ];
            if( tempIndices[ i ] > max ) {
                max = tempIndices[ i ];
            }
            if( tempIndices[ i ] < min ) {
                min = tempIndices[ i ];
            }
        }
        float average = ( 1.0f * sum ) / tempIndices.Length;
        //Debug.Log( "Average number of triangles touching per vertex = " + average );
        //Debug.Log( "Total number of triangles touching per vertex = " + sum );
        //Debug.Log( "Max number of triangles touching per vertex = " + max );
        //Debug.Log( "Min number of triangles touching per vertex = " + min );

        // Step 1.5 Compute normal of each triangle.
        Vector3[] normals = new Vector3[ numTriangles ];
        for( int t = 0; t < numTriangles; t++ ) {
            for( int i = 0; i < 3; i++ ) {
                normals[t] = Vector3.Cross( vertices[ triangles[ t * 3 + 1 ] ] - vertices[ triangles[ t * 3 ] ],
                                                    vertices[ triangles[ t * 3 + 2 ] ] - vertices[ triangles[ t * 3 ] ] );
                normals[t] = Vector3.Normalize( normals[t] );
            }
        }

        // Step 2. For each triangle, 
        //          take the 3 pairs of vertices,
        //          compute the largest dot product between triangles touching the pair.
        float MAXIMUM_DOT = 0.95f;
        float sumDots = 0.0f;
        int numDots = 0;
        int numDeletedTriangles = 0;
        int numDeletedVertices = 0;
        bool alternate = true;
        for( int t = 0; t < numTriangles; t++ ) {
            for( int i = 0; i < 3; i++ ) {
                if( triangles[ t * 3 ] == -1 || triangles[ t * 3 + 1 ] == -1 || triangles[ t * 3 + 2 ] == -1 ) {
                    break;
                }
                int vertexOne = triangles[ t * 3 + i ];
                int vertexTwo = triangles[ t * 3 + ( ( i + 1 ) % 3 ) ];
                if( alternate ) {
                    vertexOne = triangles[ t * 3 + ( ( i + 1 ) % 3 ) ];
                    vertexTwo = triangles[ t * 3 + i ];
                }
                alternate = !alternate;
                if( (vertices[ vertexOne ].x == -1 && vertices[ vertexOne ].y == -1 && vertices[ vertexOne ].z == -1)
                    || ( vertices[ vertexTwo ].x == -1 && vertices[ vertexTwo ].y == -1 && vertices[ vertexTwo ].z == -1 ) ) {
                    continue;
                }
                if( tempIndices[ vertexOne ] + tempIndices[ vertexTwo ] >= MAX_TRIANGLES - 1) {
                    continue;
                }

                float maximumDot = 999f;
                int comparisons = 0;
                for( int a = 0; a < tempIndices[ vertexOne ] - 1; a++ ) {

                    int triangleOne = vertexTriangles[ vertexOne * MAX_TRIANGLES + a ];
                    Vector3 triangleOneNormal = normals[ triangleOne ];

                    for( int b = a + 1; b < tempIndices[ vertexOne ]; b++ ) {
                        int triangleTwo = vertexTriangles[ vertexTwo * MAX_TRIANGLES + b ];
                        if( triangleOne != triangleTwo ) {
                            Vector3 triangleTwoNormal = normals[ triangleTwo ];

                            float dot = Vector3.Dot( triangleOneNormal, triangleTwoNormal );
                            comparisons++;
                            //float dot = Mathf.Abs( Vector3.Dot( triangleOneNormal, triangleTwoNormal ) );
                            if( dot < maximumDot || maximumDot == 999f) {
                                maximumDot = dot;
                            }
                        }
                    }
                }
                sumDots += maximumDot;
                numDots++;
                if( maximumDot > MAXIMUM_DOT && maximumDot != 999 && tempIndices[ vertexOne ] >= 4 ) {
                    file.WriteLine( "maximumDot = " + maximumDot );
                    //Debug.Log( "Between vertex " + vertexOne + " and " + vertexTwo );
                    // area is flat enough, combine vertex one and two.
                    numDeletedVertices++;
                    // delted vertex two, now look through the triangles touching vertex two.
                    for( int b = 0; b < tempIndices[ vertexTwo ]; b++ ) {
                        int triangleIndex = vertexTriangles[ vertexTwo * MAX_TRIANGLES + b ];
                        if( triangles[ triangleIndex * 3 ] == -1 || triangles[ triangleIndex * 3 + 1 ] == -1 || triangles[ triangleIndex * 3 + 2 ] == -1 ) {
                            // if this triangle has already been deleted, skip
                            continue;
                        }
                        file.WriteLine( "triangle two= " + vertices[ triangles[ triangleIndex * 3 ] ] + "," + vertices[ triangles[ triangleIndex * 3 + 1 ] ] + "," + vertices[ triangles[ triangleIndex * 3 + 2 ] ] );
                        bool deletedTriangle = false;
                        for( int j = 0; j < 3; j++ ) {
                            // delete the triangles that connect to both vertex one and two.
                            if( triangles[ triangleIndex * 3 + j ] == vertexOne ) {
                                deletedTriangle = true;
                                break;
                            }
                            // change the triangle vertex from two to one since two is deleted.
                            else if( triangles[ triangleIndex * 3 + j ] == vertexTwo ) {
                                file.WriteLine( "About to switch vertex from two to one: " + vertexTwo + "->" + vertexOne + " = " + vertices[ vertexTwo ] + "->" + vertices[ vertexOne ] );
                                file.WriteLine( "Current triangle = " + triangles[ triangleIndex * 3 ] + "," + triangles[ triangleIndex * 3 + 1 ] + "," + triangles[ triangleIndex * 3 + 2 ] );
                                file.Write( "vertexTriangles[ vertexTwo * MAX_TRIANGLES + b ] = " + vertexTriangles[ vertexTwo * MAX_TRIANGLES + b ] );
                                file.WriteLine( "\tb = " + b );
                                triangles[ triangleIndex * 3 + j ] = vertexOne;

                                vertexTriangles[ vertexOne * MAX_TRIANGLES + tempIndices[ vertexOne ] ] = triangleIndex;
                                tempIndices[ vertexOne ]++;
                                file.WriteLine( "vertex One Now has " + tempIndices[ vertexOne ] + " touching triangles" );
                                if( tempIndices[ vertexOne ] > MAX_TRIANGLES ) {
                                    file.WriteLine( "ERROR Too many touching triangles" );
                                }
                            }
                        }
                        if( deletedTriangle ) {
                            numDeletedTriangles++;
                            for( int j = 0; j < 3; j++ ) {
                                triangles[ triangleIndex * 3 + j ] = -1;
                            }
                        }
                    }
                    vertices[ vertexTwo ] = new Vector3( -1, -1, -1 );
                    for( int a = 0; a < tempIndices[ vertexOne ]; a++ ) {
                        int triangleOne = vertexTriangles[ vertexOne * MAX_TRIANGLES + a ];
                        if( triangles[ triangleOne * 3 ] == -1 || triangles[ triangleOne * 3 + 1 ] == -1 || triangles[ triangleOne * 3 + 2 ] == -1 ) {
                            continue;
                        }
                        file.WriteLine( "post triangle one= " + vertices[ triangles[ triangleOne * 3 ] ] + "," + vertices[ triangles[ triangleOne * 3 + 1 ] ] + "," + vertices[ triangles[ triangleOne * 3 + 2 ] ] );
                    }
                    for( int b = 0; b < tempIndices[ vertexTwo ]; b++ ) {
                        int triangleIndex = vertexTriangles[ vertexTwo * MAX_TRIANGLES + b ];
                        if( triangles[ triangleIndex * 3 ] == -1 || triangles[ triangleIndex * 3 + 1 ] == -1 || triangles[ triangleIndex * 3 + 2 ] == -1 ) {
                            continue;
                        }
                        file.WriteLine( "post triangle two= " + vertices[ triangles[ triangleIndex * 3 ] ] + "," + vertices[ triangles[ triangleIndex * 3 + 1 ] ] + "," + vertices[ triangles[ triangleIndex * 3 + 2 ] ] );

                    }
                }
            }
        }
        float avgDots = sumDots / numDots;
        Debug.Log( "Total dot product = " + sumDots );
        Debug.Log( "Number of dot products = " + numDots );
        Debug.Log( "Average of dot products = " + avgDots );
        Debug.Log( "Number of deleted vertices = " + numDeletedVertices );
        Debug.Log( "Number of deleted triangles = " + numDeletedTriangles );

        int[] conversion = new int[ vertices.Length ];

        for( int i = 0; i < conversion.Length; i++ ) { conversion[ i ] = 0; }
        for( int index = 0; index < triangles.Length; index++ ) {
            if( triangles[ index ] != -1 ) {
                int oldVertexIndex = triangles[ index ];
                conversion[ oldVertexIndex ]++;
            }
        }
        int numVer = 0;
        for( int i = 0; i < conversion.Length; i++ ) {
            if( conversion[i] != 0 ) {
                numVer++;
            }
        }

        Debug.Log( "Before polish Number of vertices = " + vertices.Length );
        Debug.Log( "           Number of triangles = " + triangles.Length / 3 );
        Debug.Log( "           Number of vertexes referenced = " + numVer );

        for( int i = 0; i < conversion.Length; i++ ) { conversion[ i ] = -1; }
        for( int index = 0; index < triangles.Length; index++ ) {
            if( triangles[ index ] != -1 ) {
                int oldVertexIndex = triangles[ index ];
                Vector3 oldVertex = vertices[ oldVertexIndex ];
                if( conversion[ oldVertexIndex ] == -1 ) {
                    newVertices.Add( oldVertex );
                    conversion[ oldVertexIndex ] = newVertices.Count - 1;
                }
                newTriangles.Add( conversion[ oldVertexIndex ] );
            }
        }
        Debug.Log( "Ended with Number of vertices = " + newVertices.Count );
        Debug.Log( "           Number of triangles = " + newTriangles.Count/3 );
        Debug.Log( "Finished mesh reduction" );

        file.Close();
    }
}
