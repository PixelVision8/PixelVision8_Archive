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
        private PixelData<int> pattern = new PixelData<int>();

        public void SetPattern(int[] newPixels, int newWidth, int newHeight)
        {
            var newRequest = NextRequest();

            if (newRequest == null)
                return;

            newRequest.Action = SetPatternAction;

            if (newRequest.PixelData.Width != newWidth || newRequest.PixelData.Height != newHeight)
            {
                Utilities.Resize(newRequest.PixelData, newWidth, newHeight);
            }

            newRequest.PixelData.SetPixels(newPixels, newWidth, newHeight);

            // Save the changes to the request
            requestPool[currentRequest] = newRequest;
        }

        [DrawAction]
        public void SetPatternAction(CanvasDrawRequest request) => pattern.SetPixels(request.PixelData.Pixels, request.PixelData.Width, request.PixelData.Height);
        
    }
}