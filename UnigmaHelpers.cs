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
}
