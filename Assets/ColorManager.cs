using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;


public class ColorManager : NetworkBehaviour
{
    public Color[] Colors;
    public bool[] colorInUse;

    // Start is called before the first frame update
    void Start()
    {
        // Keep the color manager through level changes
        DontDestroyOnLoad(this);
    }

    public Color GetColor(int colorIndex)
    {
        // return the actual color associated with this color index
        return Colors[colorIndex];
    }

    [Server]
    public void ReleaseColorIndex(int colorIndex)
    {
        colorInUse[colorIndex] = false;
    }

    [Server]
    public int ReserveColorIndex()
    {
        int colorIndex = 0;

        // try to find a color index randomly first
        for (int count = 0; count < 100; count++)
        {
            // pick a random bright color index
            colorIndex = (int)Random.Range(0.0f, Colors.Length - 0.01f);

            // check if it is in use
            if (colorInUse[colorIndex] == false)
            {
                // color not in use, mark as in use and return it to the caller 
                colorInUse[colorIndex] = true;

                // reserve this color
                return colorIndex;
            }
        }


        // go through all the colors in use table
        for (int i = 0; i < colorInUse.Length; i++)
        {
            // just chose the first color not in use
            if (colorInUse[i] == false)
            {
                // color not in use, mark as in use and return it to the caller 
                colorInUse[colorIndex] = true;

                // reserve this color
                return colorIndex;
            }
        }

        // if all else fails return the first color index
        return 0;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
