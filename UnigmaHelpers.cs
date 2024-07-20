using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UnigmaHelpers{

    public static void PrintOutMatrix(float[] result, int row, int col)
    {
        //Log the matrix in matrix notation and format.
        string matrix = "";
        for (int i = 0; i < row; i++)
        {
            matrix += "[";
            for (int j = 0; j < col; j++)
            {
                //Ensures element exists
                if (j + i * row >= result.Length)
                {
                    matrix += '0';
                }
                else
                    matrix += result[j + i * row];

                if (j != col - 1)
                {
                    matrix += ", ";
                }
            }
            matrix += "]\n";
        }
        Debug.Log(matrix);
    }

    public static void PrintOutMatrix(Matrix4x4 mat)
    {
        string matS = $"{mat.m00}, {mat.m01}, {mat.m02}, {mat.m03}" + "\n" + $"{mat.m00}, {mat.m01}, {mat.m02}, {mat.m03}" + "\n" + $"{mat.m20}, {mat.m21}, {mat.m22}, {mat.m23}" + "\n" + $"{mat.m30}, {mat.m31}, {mat.m32}, {mat.m33}";

        Debug.Log(matS);
    }
}
