using System;
using System.Collections.Generic;

namespace PixelVision8.Runner
{
    public partial class SystemParser
    {
        [ChipParser("SpriteChip")]
        public void ConfigureSpriteChip(Dictionary<string, object> data)
        {
            var spriteChip = Target.SpriteChip;
            var displayChip = Target.DisplayChip;

            // Flag chip to export
            //spriteChip.export = true;

            if (data.ContainsKey("maxSpriteCount")) displayChip.MaxDrawRequests = (int) (long) data["maxSpriteCount"];

            if (data.ContainsKey("cps")) spriteChip.ColorsPerSprite = (int) (long) data["cps"];

            if (data.ContainsKey("pages")) spriteChip.Pages = (int) (long) data["pages"];

        }
    }
}