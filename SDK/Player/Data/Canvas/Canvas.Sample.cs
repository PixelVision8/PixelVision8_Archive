//   
// Copyright (c) Jesse Freeman, Pixel Vision 8. All rights reserved.  
//  
// Licensed under the Microsoft Public License (MS-PL) except for a few
// portions of the code. See LICENSE file in the project root for full 
// license information. Third-party libraries used by Pixel Vision 8 are 
// under their own licenses. Please refer to those libraries for details 
// on the license they use.
// 
// Contributors
// --------------------------------------------------------
// This is the official list of Pixel Vision 8 contributors:
//  
// Jesse Freeman - @JesseFreeman
// Christina-Antoinette Neofotistou @CastPixel
// Christer Kaitila - @McFunkypants
// Pedro Medeiros - @saint11
// Shawn Rakowski - @shwany
//

namespace PixelVision8.Player
{
    public sealed partial class Canvas
    {
        public void SetPixelAt(int x, int y, int value)
        {
            var index = x + y * Width;
            defaultLayer[index] = value;
        }

        public int ReadPixelAt(int x, int y)
        {
            // Calculate the index
            var index = x + y * Width;
        
            if (index >= defaultLayer.Total) return Constants.EmptyPixel;
        
            // Flatten the canvas
            Draw();
        
            return defaultLayer[index];
        }

        public int[] SamplePixels(int x, int y, int width, int height)
        {
            return GetPixels(x, y, width, height);
        }
        
    }
}