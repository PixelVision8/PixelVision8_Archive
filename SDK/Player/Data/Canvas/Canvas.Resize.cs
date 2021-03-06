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

using System;

namespace PixelVision8.Player
{
    public sealed partial class Canvas
    {
        public int[] ResizePixels(int[] pixels, int w1, int h1, int w2, int h2)
        {
            int[] temp = new int[w2*h2] ;
            double x_ratio = w1/(double)w2 ;
            double y_ratio = h1/(double)h2 ;

            double px, py ; 
            for (int i=0;i<h2;i++) {
                for (int j=0;j<w2;j++) {
                    px = Math.Floor(j*x_ratio) ;
                    py = Math.Floor(i*y_ratio) ;
                    temp[(i*w2)+j] = pixels[(int)((py*w1)+px)] ;
                }
            }
            return temp ;
        }
    }
}