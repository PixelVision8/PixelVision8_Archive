﻿//   
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
using System.Linq;
using Microsoft.Xna.Framework;
using PixelVision8.Engine.Utils;

namespace PixelVision8.Engine.Chips
{
    /// <summary>
    ///     The <see cref="SpriteChip" /> represents a way to store and retrieve
    ///     sprite pixel data. Internally, the pixel data is stored in a
    ///     <see cref="TextureData" /> class allowing you to dynamically resize
    ///     sprites at run time.
    /// </summary>
    public class SpriteChip : AbstractChip
    {
        // TODO these are hard coded assuming the sprites are always 8x8
        protected readonly int[] emptySpriteData = Enumerable.Repeat(-1, 64).ToArray();

        protected int _colorsPerSprite = 8;
        protected int _pages;

        private Image spriteMemory = new Image(128, 128);

        /// <summary>
        ///     Internal <see cref="cache" /> for faster lookup
        /// </summary>
        protected string[] cache;

        public readonly int pageHeight = 128;

        //protected Vector2 pageSize = new Vector2(128, 128);
        public readonly int pageWidth = 128;

        protected int tmpX;
        protected int tmpY;
        // protected int totalSprites1;
        public bool unique = false;
        protected int w;
        protected int width1;

        /// <summary>
        ///     Sets the total number of sprite draw calls for the display.
        /// </summary>
        public int maxSpriteCount { get; set; }

        /// <summary>
        ///     The global <see cref="width" /> of sprites in the engine. By default
        ///     this is set to 8.
        /// </summary>
        public int width
        {
            get => spriteMemory.SpriteWidth;
            set => spriteMemory.SpriteWidth = value;
        }

        /// <summary>
        ///     The global <see cref="width" /> of sprites in the engine. By default
        ///     this is set to 8.
        /// </summary>
        public int height
        {
            get => spriteMemory.SpriteHeight;
            set => spriteMemory.SpriteHeight = value;
        }

        /// <summary>
        ///     A public getter for the internal
        ///     TextureData. When requested, a clone of the <see cref="_texture" />
        ///     field is returned. This is expensive and only used for tools.
        /// </summary>
        // public TextureData texture
        // {
        //     get => _texture;
        //     set => SpriteChipUtil.CloneTextureData(value, _texture);
        //     //TODO do we need this?
        // }

        public PixelData PixelData
        {
            get => spriteMemory.PixelData;
        }

        /// <summary>
        ///     Return's the <see cref="Sprite" /> Ram's internal
        ///     <see cref="texture" /> <see cref="width" />
        /// </summary>
        public int textureWidth => spriteMemory.Width;

        /// <summary>
        ///     Return's the <see cref="Sprite" /> Ram's internal
        ///     <see cref="texture" /> <see cref="width" />
        /// </summary>
        public int textureHeight => spriteMemory.Height;

        /// <summary>
        ///     The virtual number of pages of sprite memory the SpriteChip
        ///     has. Each page contains a max number of sprites.
        /// </summary>
        public int pages
        {
            get => _pages;
            set
            {
                if (_pages == value) return;

                _pages = MathHelper.Clamp(value, 1, 8);
                
                spriteMemory.Resize(
                    (int) Math.Ceiling((float) pageWidth / width) * width, 
                    (int) Math.Ceiling((float) pageHeight * pages / height)* height
                    );
               
                cache = new string[spriteMemory.TotalSprites];

            }
        }

        /// <summary>
        ///     Total number of sprites that can be stored in memory.
        /// </summary>
        public int TotalSprites => spriteMemory.TotalSprites;

        /// <summary>
        ///     Total number of sprites that exist in memory.
        /// </summary>
        public int SpritesInMemory => cache.Count(x => x != null);

        /// <summary>
        ///     Number of colors per sprite.
        /// </summary>
        public int colorsPerSprite
        {
            get => _colorsPerSprite;
            set => _colorsPerSprite = MathHelper.Clamp(value, 1, 16);
        }

        /// <summary>
        ///     Tests to see if sprite <paramref name="data" /> is empty. This method
        ///     iterates over all the ints in the supplied <paramref name="data" />
        ///     array and looks for a value of -1. If all values are -1 then it
        ///     returns true.
        /// </summary>
        /// <param name="data">An array of ints</param>
        /// <returns>
        /// </returns>
        public bool IsEmpty(int[] data)
        {
            var total = data.Length;
            for (var i = 0; i < total; i++)
                if (data[i] > -1)
                    return false;

            return true;
        }

        public bool IsEmptyAt(int index)
        {
            return string.IsNullOrEmpty(cache[index]);
        }

        /// <summary>
        ///     Returns the next empty id in the <see cref="cache" /> index. Used for
        ///     building tools to modify the <see cref="SpriteChip" /> like importers
        ///     or if you want to add sprite dynamically at runtime and need to know
        ///     the next open index.
        /// </summary>
        /// <returns>
        /// </returns>
        public int NextEmptyID()
        {
            return Array.FindIndex(cache, string.IsNullOrEmpty);
        }

        /// <summary>
        ///     This configures the <see cref="SpriteChip" /> when it is activated.
        ///     It registers itself with the engine as the default SpriteChip, it
        ///     calls Clear() to clear any data and also sets the defaut size to 8 x
        ///     8.
        /// </summary>
        public override void Configure()
        {
            engine.SpriteChip = this;

            pages = 4;
            // _texture.wrapMode = false;
            // width = 8;
            // height = 8;


            // Clear();
        }

        public override void Deactivate()
        {
            base.Deactivate();
            engine.SpriteChip = null;
        }

        /// <summary>
        ///     This resizes the <see cref="Sprite" /> Ram's
        ///     internal TextureData. This will destroy all
        ///     sprites in memory.
        /// </summary>
        /// <param name="w">
        ///     New <see cref="width" /> for the internal
        ///     TextureData.
        /// </param>
        /// <param name="h">
        ///     New <see cref="height" /> for the internal
        ///     TextureData.
        /// </param>
        // public void Resize(int w, int h)
        // {
        //     
        //     w = Math.Max((int) Math.Ceiling((float) w / width) * width, width);
        //     h = Math.Max((int) Math.Ceiling((float) h / height) * height, height);
        //
        //     // if (textureWidth != w || textureHeight != h)
        //     // {
        //         spriteMemory.Resize(w, h);
        //     // }
        //
        //     //TODO this needs to be double checked at different size sprites
        //     var cols = MathUtil.FloorToInt(textureWidth / width);
        //     var rows = MathUtil.FloorToInt(textureHeight / height);
        //     totalSprites1 = cols * rows;
        // }

        /// <summary>
        ///     This caches a sprite for easier look up and duplicate detection.
        ///     Each sprite is cached as a string.
        /// </summary>
        /// <param name="index">
        ///     Index where the sprite's cached value should be stored in the
        ///     <see cref="cache" /> array.
        /// </param>
        /// <param name="data">
        ///     An array of ints that represents the sprite's color data.
        /// </param>
        protected void CacheSprite(int index, int[] data)
        {
            if (index < 0 || index >= cache.Length) return;

            // TODO need to test to see if the sprite is empty first (no need to cache)

            cache[index] = SpriteChipUtil.SpriteDataToString(data);

            var totalPixels = width * height;

            var tmpPixels = new int[totalPixels];
            Array.Copy(data, tmpPixels, totalPixels);
            //            pixelDataCache[index] = tmpPixels;
        }

        /// <summary>
        ///     Clears the Image to a default color index of -1
        ///     and also resets the cache. This removes all sprites from the chip.
        /// </summary>
        // public void Clear()
        // {
        //     
        //     spriteMemory.Clear();
        // }

        /// <summary>
        ///     Returns an array of ints that represent a sprite. Each
        ///     int should be mapped to a color
        ///     <paramref name="index" /> to display in the renderer.
        /// </summary>
        /// <param name="index">
        ///     Anint representing the location in memory of the
        ///     sprite.
        /// </param>
        /// <param name="pixelData"></param>
        /// <returns>
        /// </returns>
        public void ReadSpriteAt(int index, ref int[] pixelData)
        {
            // TODO check to see if the cache doesn't exist and return the empty sprite as well
            if (index == -1)
            {
                var size = width * height;

                if (pixelData.Length < size) Array.Resize(ref pixelData, size);

                Array.Copy(emptySpriteData, pixelData, size);
            }
            else
            {
                width1 = this.spriteMemory.Width;
                //                height1 = _texture.height;

                w = width1 / width;

                tmpX = index % w * width;
                tmpY = index / w * height;

                // Flip y for Unity
                //            tmpY = height1 - tmpY - height;

                PixelDataUtil.CopyPixels(ref pixelData, this.spriteMemory.PixelData, tmpX, tmpY, width, height);
                // _texture.CopyPixels(ref pixelData, tmpX, tmpY, width, height);
            }
        }

        // public void DrawSprite(ref int[] pixelData, int index, int x, int y, bool flipX = false, bool flipY = false,
        //     int colorOffset = 0)
        // {
        //
        //
        //
        // }

        /// <summary>
        ///     Updates the sprite data at a given position in the
        ///     <see cref="texture" /> data.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="pixels"></param>
        public void UpdateSpriteAt(int index, int[] pixels)
        {
            if (index < 0) return;

            //            int spriteWidth = width;
            //            int spriteHeight = height;
            //            int x;
            //            int y;
            //            int index1 = index;
            //            int width2 = _texture.width;
            //            int height2 = _texture.height;
            //            var totalSprites2 = SpriteChipUtil.CalculateTotalSprites(width2, height2, spriteWidth, spriteHeight);

            // Make sure we stay in bounds
            index = MathHelper.Clamp(index, 0, spriteMemory.TotalSprites - 1);

            var w1 = spriteMemory.Width / width;

            tmpX = index % w1 * width;
            tmpY = index / w1 * height;

            //            if (true)
            //                y = _texture.height - y - height;

            PixelDataUtil.SetPixels(this.spriteMemory.PixelData, tmpX, tmpY, width, height, pixels);
            // _texture.SetPixels(x, y, width, height, pixels);

            CacheSprite(index, pixels);
        }

        /// <summary>
        ///     Finds a sprite by looking it up against the cache. Returns -1 if no
        ///     sprite is found. This is used for insuring duplicate sprites aren't
        ///     added to the TextureData.
        /// </summary>
        /// <param name="pixels">
        ///     An array of ints representing the sprite's color data.
        /// </param>
        /// <returns>
        /// </returns>
        public int FindSprite(int[] pixels, bool emptyCheck = false)
        {
            if (emptyCheck)
                if (IsEmpty(pixels))
                    return -1;

            var sprite = SpriteChipUtil.SpriteDataToString(pixels);

            return Array.IndexOf(cache, sprite);
        }
    }
}