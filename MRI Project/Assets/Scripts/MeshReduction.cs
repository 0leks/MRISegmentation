using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MeshReduction : MonoBehaviour {



    public static void ComputeNormalSums(Vector3[] vertices, int[] triangles, Vector3[] normals, int[] neighborCount) {
        for (int t = 0; t < triangles.Length / 3; t++) {
            if (triangles[t * 3] != -1) {
                Vector3 vec1 = vertices[triangles[t * 3 + 1]] - vertices[triangles[t * 3]];
                Vector3 vec2 = vertices[triangles[t * 3 + 2]] - vertices[triangles[t * 3]];
                vec1 = Vector3.Normalize(vec1);
                vec2 = Vector3.Normalize(vec2);
                Vector3 normal = Vector3.Cross(vec1, vec2);
                normal = Vector3.Normalize(normal);
                for (int i = 0; i < 3; i++) {
                    normals[triangles[t * 3 + i]] += normal;
                    neighborCount[triangles[t * 3 + i]]++;
                }
            }
        }
    }
    public static void ComputeMagnitudesAndNormalize(Vector3[] normals, double[] magnitudes, int[] neighborCount) {
        for (int i = 0; i < normals.Length; i++) {
            magnitudes[i] = Vector3.Magnitude(normals[i]) / neighborCount[i];
            normals[i] = Vector3.Normalize(normals[i]);
        }
    }
    public static void ComputeTriangleMagnitudes(double[] magnitudes, double[] triangleMagnitudes, int[] triangles) {
        for (int i = 0; i < triangles.Length / 3; i++) {
            double sumMag = 0;
            for (int j = 0; j < 3; j++) {
                if (triangles[i * 3 + j] != -1) {
                    sumMag += magnitudes[triangles[i * 3 + j]];
                }
            }
            triangleMagnitudes[i] = sumMag;
        }
    }
    public static void ReduceMesh( Vector3[] vertices, int[] triangles, List<Vector3> newVertices , List<int> newTriangles, List<Vector3> newNormals, List<Color32> newColors) {

        if (triangles.Length % 3 != 0) {
            Debug.LogError("ERROR! size of triangle array is not multiple of 3!!!!!!!");
            return;
        }

        StreamWriter file = new StreamWriter( Application.dataPath + "/test2.txt" );
        Debug.Log( "Beginning mesh reduction" );
        Debug.Log( "Started with Number of vertices = " + vertices.Length );
        Debug.Log( "             Number of triangles = " + triangles.Length );

        Vector3[] normals = new Vector3[vertices.Length];
        Color32[] colors = new Color32[vertices.Length];
        int[] neighborCount = new int[vertices.Length];
        double[] magnitudes = new double[normals.Length];

        ComputeNormalSums(vertices, triangles, normals, neighborCount);
        ComputeMagnitudesAndNormalize(normals, magnitudes, neighborCount);

        // no mesh reduction, simply compute normals and assign vertex colors.
        bool finished = true;
        int counter = 20;
        while (!finished) {
            counter--;
            if (counter < 0) {
                break;
            }
            normals = new Vector3[vertices.Length];
            neighborCount = new int[vertices.Length];

            Debug.Log("Computing normals");
            ComputeNormalSums(vertices, triangles, normals, neighborCount);

            Debug.Log("Computing magnitudes and normalizing normals");
            ComputeMagnitudesAndNormalize(normals, magnitudes, neighborCount);

            Debug.Log("Computing triangle magnitudes");
            double[] triangleMagnitudes = new double[triangles.Length/3];
            ComputeTriangleMagnitudes(magnitudes, triangleMagnitudes, triangles);
            
            int maximumTriangleMagnitudeIndex = 0;
            for( int i = 0; i < triangles.Length/3; i++ ) {
                if( triangleMagnitudes[i] > triangleMagnitudes[maximumTriangleMagnitudeIndex] ) {
                    maximumTriangleMagnitudeIndex = i;
                }
            }

            // DEBUG OUTPUT
            for (int t = 0; t < triangles.Length / 3; t++) {
                file.Write("Triangle " + t + " ");
                file.Write("Vertexes: " + triangles[t * 3] + ", " + triangles[t * 3 + 1] + ", " + triangles[t * 3 + 2] + " ");
                if (triangles[t * 3] != -1) {
                    file.Write(vertices[triangles[t * 3]] + ", " + vertices[triangles[t * 3 + 1]] + ", " + vertices[triangles[t * 3 + 2]] + " ");
                    file.Write("mag=" + triangleMagnitudes[t] + " ");
                    //file.Write("Normal: " + normals[t] + " ");
                }
                file.WriteLine();
            }

            if (triangleMagnitudes[maximumTriangleMagnitudeIndex] < 3 ) { // each triangle has 3 vertices so max is 1+1+1=3
                finished = true;
                break;
            }
            Debug.Log("Deciding triangle vertexes");
            int a = Random.Range(0, 3);
            int b = Random.Range(0, 2) + 1;
            if( a == b ) {
                b = 0;
            }

            a = triangles[maximumTriangleMagnitudeIndex * 3 + a];
            b = triangles[maximumTriangleMagnitudeIndex * 3 + b];
            Debug.Log("Merging vertex " + a + " and " + b);
            
            //for (int i = 0; i < normals.Length; i++) {
            //    //Debug.Log("cur=" + neighborCount[i]);
            //    //float col = (float)((neighborCount[i]-minN) * 1.0f / (maxN-minN));
            //    //Debug.Log("cur=" + magnitudes[i]);
            //    float col = (float)((magnitudes[i] - min) * 1.0f / (max - min));
            //    colors[i] = new Color(col, 1 - col, 1 - col, 1.0f);
            //    //colors[i] = new Color(normals[i].x/2 + 0.5f, normals[i].y / 2 + 0.5f, normals[i].z / 2 + 0.5f);
            //    //if( magnitudes[i] == max ) {
            //    //    colors[i] = new Color(1.0f, 0, 0, 1.0f);

            //    //}
            //    //if (magnitudes[i] <= min + 0.01f) {
            //    //    colors[i] = new Color(0, 0, 1.0f, 1.0f);
            //    //}
            //    //if( i >= 42 && i < 50) {
            //    //    colors[i] = new Color(0, 1.0f, 0, 1.0f);
            //    //}
            //    //if (i == 49) {
            //    //    colors[i] = new Color(0, 0, 0, 1.0f);
            //    //}
            //}

            Debug.Log("Computing triangleSet");
            List<HashSet<int>> triangleSet = new List<HashSet<int>>();
            for (int t = 0; t < vertices.Length; t++) {
                triangleSet.Add(new HashSet<int>());
            }
            // iterate thru triangle and add triangle to each of its 3 vertex sets.
            for (int t = 0; t < triangles.Length/3; t++) {
                if (triangles[t * 3] != -1) {
                    triangleSet[triangles[t * 3]].Add(t);
                    triangleSet[triangles[t * 3 + 1]].Add(t);
                    triangleSet[triangles[t * 3 + 2]].Add(t);
                }
            }
            for (int t = 0; t < vertices.Length; t++) {
                file.Write("Index " + t + " intersects ");
                foreach (int i in triangleSet[t]) {
                    file.Write(i + ", ");
                }
                file.WriteLine();
            }

            Debug.Log("Finding intersection of vertices");
            HashSet<int> intersection = new HashSet<int>();
            foreach (int i in triangleSet[a]) {
                intersection.Add(i);
            }
            intersection.IntersectWith(triangleSet[b]);
            file.Write("Index " + a + " and " + b + " intersection: ");
            foreach (int i in intersection) {
                file.Write(i + ", " + "{" + triangles[i * 3] + "," + triangles[i * 3 + 1] + "," + triangles[i * 3 + 2] + "}");
                colors[triangles[i * 3]] = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                colors[triangles[i * 3 + 1]] = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                colors[triangles[i * 3 + 2]] = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                // mark these triangles as garbage
                for (int index = 0; index < 3; index++) {
                    triangles[i * 3 + index] = -1;
                }
                triangleSet[a].Remove(i);
                triangleSet[b].Remove(i);
            }
            file.WriteLine();

            Vector3 newVertex = new Vector3(vertices[a].x, vertices[a].y, vertices[a].z);
            vertices[a] = newVertex;
            List<int> toRemove = new List<int>();
            foreach (int i in triangleSet[b]) {
                for (int index = 0; index < 3; index++) {
                    if (triangles[i * 3 + index] == b) {
                        file.WriteLine("changing triangle " + i + " vertex #" + index + " from " + b + " to " + a);
                        triangles[i * 3 + index] = a;
                        triangleSet[a].Add(i);
                        //triangleSet[b].Remove(i); // need to make this happen after the for loop
                        toRemove.Add(i);
                    }
                    //triangles[i * 3 + index] = 0;
                }
            }
            foreach (int i in toRemove) {
                file.WriteLine("Removing Triangle " + i + " from vertex " + b);
                triangleSet[b].Remove(i);
            }


            for (int t = 0; t < triangles.Length/3; t++) {
                file.Write("Triangle " + t + " ");
                if (triangles[t * 3] != -1) {
                    file.Write("Vertexes: " + triangles[t * 3] + ", " + triangles[t * 3 + 1] + ", " + triangles[t * 3 + 2] + " ");
                    file.Write(vertices[triangles[t * 3]] + ", " + vertices[triangles[t * 3 + 1]] + ", " + vertices[triangles[t * 3 + 2]] + " ");
                }
                else {
                    file.Write("deleted");
                }
                file.WriteLine();
            }
            for (int t = 0; t < vertices.Length; t++) {
                file.Write("Index " + t + " intersects ");
                foreach (int i in triangleSet[t]) {
                    file.Write(i + ", ");
                }
                file.WriteLine();
            }

        }


        //Now recompute error level.
        // For any vertex sharing a triangle with a or b resum normals.

        for (int i = 0; i < normals.Length; i++) {
            //Debug.Log("cur=" + neighborCount[i]);
            //float col = (float)((neighborCount[i]-minN) * 1.0f / (maxN-minN));
            //Debug.Log("cur=" + magnitudes[i]);
            //float col = (float)((magnitudes[i] - min) * 1.0f / (max - min));
            //colors[i] = new Color(col, 1 - col, 1 - col, 1.0f);
            colors[i] = new Color(normals[i].x/2 + 0.5f, normals[i].y / 2 + 0.5f, normals[i].z / 2 + 0.5f);
        }
        for (int vertex = 0; vertex < vertices.Length; vertex++) {
            newVertices.Add(vertices[vertex]);
            newNormals.Add(normals[vertex]);
            //colors[vertex] = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            newColors.Add(colors[vertex]);
        }
        for (int triangle = 0; triangle < triangles.Length / 3; triangle++) {
            if (triangles[triangle * 3] != -1) {
                newTriangles.Add(triangles[triangle * 3]);
                newTriangles.Add(triangles[triangle * 3 + 1]);
                newTriangles.Add(triangles[triangle * 3 + 2]);
            }
        }
        file.Close();

        //// Step 1. Each vertex needs a list of triangles that touch it.

        //// Each vertex can have a max of 20 triangles touching it.
        //int MAX_TRIANGLES = 100;
        //int[] vertexTriangles = new int[ vertices.Length * MAX_TRIANGLES ];
        //int[] tempIndices = new int[ vertices.Length ];
        //for( int t = 0; t < numTriangles; t++ ) {
        //    for( int i = 0; i < 3; i ++ ) {
        //        int vertexNumber = triangles[ t * 3 + i ];
        //        if( tempIndices[ vertexNumber ] >= MAX_TRIANGLES ) {
        //            Debug.LogError( "ERROR! too many triangles touching vertex " + vertexNumber );
        //        }
        //        vertexTriangles[ vertexNumber * MAX_TRIANGLES + tempIndices[ vertexNumber ] ] = t ;
        //        tempIndices[ vertexNumber ]++;
        //    }
        //}

        //int sum = 0;
        //int max = -1;
        //int min = 999999;
        //for( int i = 0; i < tempIndices.Length; i++ ) {
        //    sum += tempIndices[ i ];
        //    if( tempIndices[ i ] > max ) {
        //        max = tempIndices[ i ];
        //    }
        //    if( tempIndices[ i ] < min ) {
        //        min = tempIndices[ i ];
        //    }
        //}
        //float average = ( 1.0f * sum ) / tempIndices.Length;
        ////Debug.Log( "Average number of triangles touching per vertex = " + average );
        ////Debug.Log( "Total number of triangles touching per vertex = " + sum );
        ////Debug.Log( "Max number of triangles touching per vertex = " + max );
        ////Debug.Log( "Min number of triangles touching per vertex = " + min );

        //// Step 1.5 Compute normal of each triangle.
        //Vector3[] normals = new Vector3[ numTriangles ];
        //for( int t = 0; t < numTriangles; t++ ) {
        //    for( int i = 0; i < 3; i++ ) {
        //        normals[t] = Vector3.Cross( vertices[ triangles[ t * 3 + 1 ] ] - vertices[ triangles[ t * 3 ] ],
        //                                            vertices[ triangles[ t * 3 + 2 ] ] - vertices[ triangles[ t * 3 ] ] );
        //        normals[t] = Vector3.Normalize( normals[t] );
        //    }
        //}

        //// Step 2. For each triangle, 
        ////          take the 3 pairs of vertices,
        ////          compute the largest dot product between triangles touching the pair.
        //float MAXIMUM_DOT = 0.95f;
        //float sumDots = 0.0f;
        //int numDots = 0;
        //int numDeletedTriangles = 0;
        //int numDeletedVertices = 0;
        //bool alternate = true;
        //for( int t = 0; t < numTriangles; t++ ) {
        //    for( int i = 0; i < 3; i++ ) {
        //        if( triangles[ t * 3 ] == -1 || triangles[ t * 3 + 1 ] == -1 || triangles[ t * 3 + 2 ] == -1 ) {
        //            break;
        //        }
        //        int vertexOne = triangles[ t * 3 + i ];
        //        int vertexTwo = triangles[ t * 3 + ( ( i + 1 ) % 3 ) ];
        //        if( alternate ) {
        //            vertexOne = triangles[ t * 3 + ( ( i + 1 ) % 3 ) ];
        //            vertexTwo = triangles[ t * 3 + i ];
        //        }
        //        alternate = !alternate;
        //        if( (vertices[ vertexOne ].x == -1 && vertices[ vertexOne ].y == -1 && vertices[ vertexOne ].z == -1)
        //            || ( vertices[ vertexTwo ].x == -1 && vertices[ vertexTwo ].y == -1 && vertices[ vertexTwo ].z == -1 ) ) {
        //            continue;
        //        }
        //        if( tempIndices[ vertexOne ] + tempIndices[ vertexTwo ] >= MAX_TRIANGLES - 1) {
        //            continue;
        //        }

        //        float maximumDot = 999f;
        //        int comparisons = 0;
        //        for( int a = 0; a < tempIndices[ vertexOne ] - 1; a++ ) {

        //            int triangleOne = vertexTriangles[ vertexOne * MAX_TRIANGLES + a ];
        //            Vector3 triangleOneNormal = normals[ triangleOne ];

        //            for( int b = a + 1; b < tempIndices[ vertexOne ]; b++ ) {
        //                int triangleTwo = vertexTriangles[ vertexTwo * MAX_TRIANGLES + b ];
        //                if( triangleOne != triangleTwo ) {
        //                    Vector3 triangleTwoNormal = normals[ triangleTwo ];

        //                    float dot = Vector3.Dot( triangleOneNormal, triangleTwoNormal );
        //                    comparisons++;
        //                    //float dot = Mathf.Abs( Vector3.Dot( triangleOneNormal, triangleTwoNormal ) );
        //                    if( dot < maximumDot || maximumDot == 999f) {
        //                        maximumDot = dot;
        //                    }
        //                }
        //            }
        //        }
        //        sumDots += maximumDot;
        //        numDots++;
        //        if( maximumDot > MAXIMUM_DOT && maximumDot != 999 && tempIndices[ vertexOne ] >= 4 ) {
        //            file.WriteLine( "maximumDot = " + maximumDot );
        //            //Debug.Log( "Between vertex " + vertexOne + " and " + vertexTwo );
        //            // area is flat enough, combine vertex one and two.
        //            numDeletedVertices++;
        //            // delted vertex two, now look through the triangles touching vertex two.
        //            for( int b = 0; b < tempIndices[ vertexTwo ]; b++ ) {
        //                int triangleIndex = vertexTriangles[ vertexTwo * MAX_TRIANGLES + b ];
        //                if( triangles[ triangleIndex * 3 ] == -1 || triangles[ triangleIndex * 3 + 1 ] == -1 || triangles[ triangleIndex * 3 + 2 ] == -1 ) {
        //                    // if this triangle has already been deleted, skip
        //                    continue;
        //                }
        //                file.WriteLine( "triangle two= " + vertices[ triangles[ triangleIndex * 3 ] ] + "," + vertices[ triangles[ triangleIndex * 3 + 1 ] ] + "," + vertices[ triangles[ triangleIndex * 3 + 2 ] ] );
        //                bool deletedTriangle = false;
        //                for( int j = 0; j < 3; j++ ) {
        //                    // delete the triangles that connect to both vertex one and two.
        //                    if( triangles[ triangleIndex * 3 + j ] == vertexOne ) {
        //                        deletedTriangle = true;
        //                        break;
        //                    }
        //                    // change the triangle vertex from two to one since two is deleted.
        //                    else if( triangles[ triangleIndex * 3 + j ] == vertexTwo ) {
        //                        file.WriteLine( "About to switch vertex from two to one: " + vertexTwo + "->" + vertexOne + " = " + vertices[ vertexTwo ] + "->" + vertices[ vertexOne ] );
        //                        file.WriteLine( "Current triangle = " + triangles[ triangleIndex * 3 ] + "," + triangles[ triangleIndex * 3 + 1 ] + "," + triangles[ triangleIndex * 3 + 2 ] );
        //                        file.Write( "vertexTriangles[ vertexTwo * MAX_TRIANGLES + b ] = " + vertexTriangles[ vertexTwo * MAX_TRIANGLES + b ] );
        //                        file.WriteLine( "\tb = " + b );
        //                        triangles[ triangleIndex * 3 + j ] = vertexOne;

        //                        vertexTriangles[ vertexOne * MAX_TRIANGLES + tempIndices[ vertexOne ] ] = triangleIndex;
        //                        tempIndices[ vertexOne ]++;
        //                        file.WriteLine( "vertex One Now has " + tempIndices[ vertexOne ] + " touching triangles" );
        //                        if( tempIndices[ vertexOne ] > MAX_TRIANGLES ) {
        //                            file.WriteLine( "ERROR Too many touching triangles" );
        //                        }
        //                    }
        //                }
        //                if( deletedTriangle ) {
        //                    numDeletedTriangles++;
        //                    for( int j = 0; j < 3; j++ ) {
        //                        triangles[ triangleIndex * 3 + j ] = -1;
        //                    }
        //                }
        //            }
        //            vertices[ vertexTwo ] = new Vector3( -1, -1, -1 );
        //            for( int a = 0; a < tempIndices[ vertexOne ]; a++ ) {
        //                int triangleOne = vertexTriangles[ vertexOne * MAX_TRIANGLES + a ];
        //                if( triangles[ triangleOne * 3 ] == -1 || triangles[ triangleOne * 3 + 1 ] == -1 || triangles[ triangleOne * 3 + 2 ] == -1 ) {
        //                    continue;
        //                }
        //                file.WriteLine( "post triangle one= " + vertices[ triangles[ triangleOne * 3 ] ] + "," + vertices[ triangles[ triangleOne * 3 + 1 ] ] + "," + vertices[ triangles[ triangleOne * 3 + 2 ] ] );
        //            }
        //            for( int b = 0; b < tempIndices[ vertexTwo ]; b++ ) {
        //                int triangleIndex = vertexTriangles[ vertexTwo * MAX_TRIANGLES + b ];
        //                if( triangles[ triangleIndex * 3 ] == -1 || triangles[ triangleIndex * 3 + 1 ] == -1 || triangles[ triangleIndex * 3 + 2 ] == -1 ) {
        //                    continue;
        //                }
        //                file.WriteLine( "post triangle two= " + vertices[ triangles[ triangleIndex * 3 ] ] + "," + vertices[ triangles[ triangleIndex * 3 + 1 ] ] + "," + vertices[ triangles[ triangleIndex * 3 + 2 ] ] );

        //            }
        //        }
        //    }
        //}
        //float avgDots = sumDots / numDots;
        //Debug.Log( "Total dot product = " + sumDots );
        //Debug.Log( "Number of dot products = " + numDots );
        //Debug.Log( "Average of dot products = " + avgDots );
        //Debug.Log( "Number of deleted vertices = " + numDeletedVertices );
        //Debug.Log( "Number of deleted triangles = " + numDeletedTriangles );

        //int[] conversion = new int[ vertices.Length ];

        //for( int i = 0; i < conversion.Length; i++ ) { conversion[ i ] = 0; }
        //for( int index = 0; index < triangles.Length; index++ ) {
        //    if( triangles[ index ] != -1 ) {
        //        int oldVertexIndex = triangles[ index ];
        //        conversion[ oldVertexIndex ]++;
        //    }
        //}
        //int numVer = 0;
        //for( int i = 0; i < conversion.Length; i++ ) {
        //    if( conversion[i] != 0 ) {
        //        numVer++;
        //    }
        //}

        //Debug.Log( "Before polish Number of vertices = " + vertices.Length );
        //Debug.Log( "           Number of triangles = " + triangles.Length / 3 );
        //Debug.Log( "           Number of vertexes referenced = " + numVer );

        //for( int i = 0; i < conversion.Length; i++ ) { conversion[ i ] = -1; }
        //for( int index = 0; index < triangles.Length; index++ ) {
        //    if( triangles[ index ] != -1 ) {
        //        int oldVertexIndex = triangles[ index ];
        //        Vector3 oldVertex = vertices[ oldVertexIndex ];
        //        if( conversion[ oldVertexIndex ] == -1 ) {
        //            newVertices.Add( oldVertex );
        //            conversion[ oldVertexIndex ] = newVertices.Count - 1;
        //        }
        //        newTriangles.Add( conversion[ oldVertexIndex ] );
        //    }
        //}
        //Debug.Log( "Ended with Number of vertices = " + newVertices.Count );
        //Debug.Log( "           Number of triangles = " + newTriangles.Count/3 );
        //Debug.Log( "Finished mesh reduction" );

        //file.Close();
    }
}
