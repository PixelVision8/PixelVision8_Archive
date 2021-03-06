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

using PixelVision8.Player;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace PixelVision8.Runner
{
    public class LoadService : AbstractService
    {
        private Loader _loader;

        private BackgroundWorker loadingWorker;

        // private Color maskColor = Utilities.HexToColor("#ff00ff"); // TODO this shouldn't be hard coded 
        private AbstractParser parser;

        public PixelVision targetEngine;

        // public int TotalSteps;
        private readonly IFileLoader _fileLoadHelper;

        private IImageParser imageParser;

        public LoadService(IFileLoader fileLoadHelper)
        {
            imageParser = new PNGFileReader(fileLoadHelper);

            // TODO need to create a way to pass in the graphics device
            _loader = new Loader(fileLoadHelper, imageParser);

            _fileLoadHelper = fileLoadHelper;
        }

        public float Percent => _loader.Percent;

        /// <summary>
        ///     This can be used to display a message while preloading
        /// </summary>
        public string Message { get; protected set; }


        public virtual void ParseFiles(string[] files, PixelVision engine, FileFlags fileFlags)
        {
            // TODO need to loop through parser mappings here

            _loader.Reset();

            // Save the engine so we can work with it during loading
            targetEngine = engine;

            var test = (FileFlags) Enum.Parse(typeof(FileFlags), "System");

            // Step 1. Load the system snapshot
            if ((fileFlags & test) == test) LoadSystem(files);

            test = (FileFlags) Enum.Parse(typeof(FileFlags), "Colors");

            // Step 3 (optional). Look for new colors
            if ((fileFlags & test) == test)
            {
                // Add the color parser
                parser = LoadColors(files);
                if (parser != null) _loader.AddParser(parser);
            }

            test = (FileFlags) Enum.Parse(typeof(FileFlags), "Sprites");

            // Step 5 (optional). Look for new sprites
            if ((fileFlags & test) == test)
            {
                parser = LoadSprites(files);
                if (parser != null) _loader.AddParser(parser);
            }

            // Step 6 (optional). Look for tile map to load
            if ((fileFlags & FileFlags.Tilemap) == FileFlags.Tilemap)
            {

                var flagPath = "/Game/flags.png";

                if(files.Contains(flagPath) == false)
                {
                    flagPath = "/App/Sprites/flags.png";
                }

                if(files.Contains(flagPath))
                {
                    _loader.ParseFlagImage(flagPath, targetEngine);
                }

                LoadTilemap(files);

            } 

            // Step 7 (optional). Look for fonts to load
            if ((fileFlags & FileFlags.Fonts) == FileFlags.Fonts)
            {
                // these are the defaul font names
                var defaultFonts = new string[]
                {
                    "large",
                    "medium",
                    "small"
                };

                // Get the list of fonts in the directory
                var paths = files.Where(s => s.EndsWith(".font.png")).ToList();

                // Make sure the default fonts are either in the project or in /App/Fonts/*
                foreach (var font in defaultFonts)
                {
                    if (paths.Contains("/Game/" + font + ".font.png") == false)
                    {
                        paths.Add("/App/Fonts/" + font + ".font.png");
                    }
                }

                // Loop through each of the fonts and load them up
                foreach (var fileName in paths)
                {
                    _loader.ParseFonts(fileName, targetEngine);
                }
            }

            // Step 8 (optional). Look for meta data and override the game
            if ((fileFlags & FileFlags.Meta) == FileFlags.Meta)
            {
                parser = LoadMetaData(files);
                if (parser != null) _loader.AddParser(parser);
            }

            // Step 9 (optional). Look for meta data and override the game
            if ((fileFlags & FileFlags.Sounds) == FileFlags.Sounds)
            {
                LoadSounds(files);

                // Get all of the wav files
                var wavFiles = files.Where(x => x.EndsWith(".wav")).ToArray();

                for (int i = 0; i < wavFiles.Length; i++)
                {
                    _loader.AddParser(new WavParser(wavFiles[i], _fileLoadHelper, targetEngine.SoundChip));
                }
            }

            // Step 10 (optional). Look for meta data and override the game
            if ((fileFlags & FileFlags.Music) == FileFlags.Music) LoadMusic(files);

            // Step 11 (optional). Look for meta data and override the game
            if ((fileFlags & FileFlags.SaveData) == FileFlags.SaveData) LoadSaveData(files);

            // Step 12 (optional). Look for meta sprites
            if ((fileFlags & FileFlags.MetaSprites) == FileFlags.MetaSprites) LoadMetaSprites(files);

            // ParseExtraFileTypes(files, engine, fileFlags);
        }

        // public virtual void ParseExtraFileTypes(string[] files, IPlayerChips engine, FileFlags fileFlags)
        // {
        //     // TODO Override and add extra file parsers here.
        // }

        public void LoadAll()
        {
            _loader.LoadAll();
        }

        public void Reset()
        {
            _loader.Reset();
        }

        public void AddParser(AbstractParser parser)
        {
            _loader.AddParser(parser);
        }

        public void StartLoading()
        {
            loadingWorker = new BackgroundWorker
            {
                // TODO need a way to of locking this.

                WorkerSupportsCancellation = true,
                WorkerReportsProgress = true
            };

            loadingWorker.DoWork += WorkerLoaderSteps;
            //            bgw.ProgressChanged += WorkerLoaderProgressChanged;
            loadingWorker.RunWorkerCompleted += WorkerLoaderCompleted;
            //            bgw.WorkerReportsProgress = true;
            loadingWorker.RunWorkerAsync();
        }

        protected void WorkerLoaderSteps(object sender, DoWorkEventArgs e)
        {
            for (var i = 0; i <= _loader.TotalSteps; i++) //some number (total)
            {
                _loader.NextParser();
                Thread.Sleep(1);
                loadingWorker.ReportProgress((int) (_loader.Percent * 100), i);
            }
        }

        protected void WorkerLoaderCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // TODO need a way to tell if this failed

            if (e.Error != null)
            {
                //                DisplayError(RunnerGame.ErrorCode.Exception, new Dictionary<string, string>(){{"@{error}","There was a error while loading. See the log for more information."}}, e.Error);
            }
        }

        protected AbstractParser LoadMetaData(string[] files)
        {
            var file = files.FirstOrDefault(x => x.EndsWith("info.json"));

            if (!string.IsNullOrEmpty(file))
            {
                // var fileContents = Encoding.UTF8.GetString(ReadAllBytes(file));

                _loader.ParseMetaData(file, targetEngine);
                // return new MetaDataParser(file, _fileLoadHelper, targetEngine);
            }

            return null;
        }

        protected void LoadTilemap(string[] files)
        {
            // Console.WriteLine("LoadTilemap");

            // If a tilemap json file exists, try to load that
            var file = files.FirstOrDefault(x => x.EndsWith("tilemap.json"));

            if (!string.IsNullOrEmpty(file))
            {

                _loader.ParseTilemapJson(file, targetEngine);

                return;
            }

            // Look for default tilemap to start with
            var tilemapFiles = new List<string>();

            // Loop through all of the files
            for (int i = 0; i < files.Length; i++)
            {
                
                file = files[i];

                if(file.EndsWith(".png"))
                {

                    if(file.StartsWith("/Game/tilemap."))
                    {
                        tilemapFiles.Add(file);
                        // Console.WriteLine("Add tmp map file " + file);
                    }
                    else if(file.StartsWith("/Game/Tilemaps/"))
                    {
                        ParseTilemapFile(file);
                    }

                }

            }

            if(tilemapFiles.IndexOf("/Game/tilemap.png") > -1)
            {
                foreach (var path in tilemapFiles)
                {
                    ParseTilemapFile(path);
                }
            }

        
        }

        protected void ParseTilemapFile(string file)
        {

            // Console.WriteLine("TILEMAP FILE - " + file);

            if(file.EndsWith(".flags.png"))
            {
                // TODO need to make
                _loader.ParseTilemapFlagImage(file, targetEngine);
            }
            else
            {
                _loader.ParseTilemapImage(file, targetEngine); 
            }
        }

        protected AbstractParser LoadSprites(string[] files)
        {
            // Find the sprite file if one exists    
            var file = files.FirstOrDefault(x => x.EndsWith("sprites.png"));

            // Load the sprites.png file first
            if (!string.IsNullOrEmpty(file))
            {
                _loader.ParseSprites(file, targetEngine);
            }

            // Loop through all the remaining PNGs and make sure they should be parsed as sprites
            for (int i = 0; i < files.Length; i++)
            {
                
                file = files[i];

                if(file.StartsWith("/Game/Sprites/") && file.EndsWith(".png"))
                {
                    // total ++;
                    _loader.ParseSpritesFromFolder(file, targetEngine);
                }

            }

            return null;
        }

        protected AbstractParser LoadColors(string[] files)
        {
            // var fileName = "colors.png";


            var file = files.FirstOrDefault(x => x.EndsWith("colors.png"));

            if (!string.IsNullOrEmpty(file))
            {
                _loader.ParseColors(file, targetEngine);

                //                var tex = ReadTexture(ReadAllBytes(file));
                // var imageParser = new PNGFileReader(_fileLoadHelper);

                // return new ColorParser(file, imageParser, targetEngine.ColorChip);
                // {
                //     SourcePath = file
                // };
            }

            return null;
        }

        protected void LoadSystem(string[] files)
        {
            // var fileName = ;

            var file = files.FirstOrDefault(x => x.EndsWith("data.json"));

            if (!string.IsNullOrEmpty(file))
            {
                _loader.ParseSystem(file, targetEngine);
            }
        }

        protected void LoadSounds(string[] files)
        {
            var file = files.FirstOrDefault(x => x.EndsWith("sounds.json"));


            if (!string.IsNullOrEmpty(file))
            {
                _loader.ParseSounds(file, targetEngine);
            }
        }

        protected void LoadMusic(string[] files)
        {
            // var fileName = ;

            var file = files.FirstOrDefault(x => x.EndsWith("music.json"));


            if (!string.IsNullOrEmpty(file))
            {
                _loader.ParseMusic(file, targetEngine);
            }
        }

        protected void LoadMetaSprites(string[] files)
        {
            // var fileName = ;
            var file = files.FirstOrDefault(x => x.EndsWith("meta-sprites.json"));

            if (!string.IsNullOrEmpty(file))
            {
                _loader.ParseMetaSprites(file, targetEngine);
            }
        }

        protected void LoadSaveData(string[] files)
        {
            var file = files.FirstOrDefault(x => x.EndsWith("saves.json"));

            if (!string.IsNullOrEmpty(file))
            {
                _loader.ParseSaveData(file, targetEngine);
            }
        }
    }

    // Custom parsers
    public partial class Loader
    {
        [FileParser("saves.json", FileFlags.SaveData)]
        public void ParseSaveData(string file, PixelVision engine)
        {
            AddParser(new SystemParser(file, _fileLoadHelper, engine));
        }

        [FileParser("sounds.json", FileFlags.Sounds)]
        public void ParseSounds(string file, PixelVision engine)
        {
            AddParser(new SystemParser(file, _fileLoadHelper, engine));
        }

        [FileParser("music.json", FileFlags.Music)]
        public void ParseMusic(string file, PixelVision engine)
        {
            AddParser(new SystemParser(file, _fileLoadHelper, engine));
        }

        [FileParser("meta-sprites.json", FileFlags.MetaSprites)]
        public void ParseMetaSprites(string file, PixelVision engine)
        {
            AddParser(new SystemParser(file, _fileLoadHelper, engine));
        }
    }
}