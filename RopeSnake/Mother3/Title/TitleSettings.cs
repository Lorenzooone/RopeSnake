using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RopeSnake.Mother3.Title
{
    class TitleSettings
    {
        public bool Disclaimer;
        public bool Health;
        public bool GBAPlayer;
        public bool Title_Screen;
        public static TitleSettings DefaultSettings(bool isEnglish, bool isV12)
        {
            if (isEnglish && isV12)
                return new TitleSettings(true, true, true, true);
            else
                return new TitleSettings(true, true, true);
        }
        public TitleSettings(bool Disclaimer, bool Health, bool GBAPlayer, bool Title_Screen)
        {
            this.Disclaimer = Disclaimer;
            this.Health = Health;
            this.GBAPlayer = GBAPlayer;
            this.Title_Screen = Title_Screen;
        }
        public TitleSettings(bool Health, bool GBAPlayer, bool Title_Screen):this(false, Health, GBAPlayer, Title_Screen)
        {
        }
    }
}
