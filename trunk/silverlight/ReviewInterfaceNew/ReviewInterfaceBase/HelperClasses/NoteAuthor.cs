using System;
using System.Windows.Media;

namespace ReviewInterfaceBase.HelperClasses
{
    public class NoteAuthor
    {
        static int authorsSoFar = -1;
        private string name;
        private Classification role;
        private Brush noteBrush = new SolidColorBrush(Color.FromArgb(255, 255, 220, 0));
        private Brush headerBrush = new SolidColorBrush(Color.FromArgb(255, 235, 200, 0));
        private Brush borderBrush = new SolidColorBrush(Color.FromArgb(255, 225, 190, 0));
        private Brush lineBrush = new SolidColorBrush(Color.FromArgb(100, 255, 0, 0));
        private Brush textBrush = new SolidColorBrush(Color.FromArgb(100, 255, 0, 0));

        public int AuthorsSoFar
        {
            get
            {
                //because autherSoFar counts from 0 on but it is more standard for a count to start at 1 and on
                return authorsSoFar + 1;
            }
        }

        public Brush NoteBrush
        {
            get
            {
                return noteBrush;
            }
        }

        public Brush HeaderBrush
        {
            get
            {
                return headerBrush;
            }
        }

        public Brush BorderBrush
        {
            get
            {
                return borderBrush;
            }
        }

        public Brush LineBrush
        {
            get
            {
                return lineBrush;
            }
        }

        public Brush TextBrush
        {
            get
            {
                return textBrush;
            }
        }

        public Classification Role
        {
            get
            {
                return role;
            }
        }

        public string Name
        {
            get
            {
                return name;
            }
        }

        public NoteAuthor(Classification role, string name = null)
        {
            authorsSoFar++;
            this.role = role;

            if (name == null)
            {
                name = "Anonyms";
            }
            else
            {
                this.name = name;
            }

            SetBrushes(GetBaseColor());
        }

        private void SetBrushes(Color baseColor)
        {
            noteBrush = new SolidColorBrush(baseColor);
            headerBrush = new SolidColorBrush(baseColor);
            borderBrush = new SolidColorBrush(baseColor);

            baseColor.A = 100;
            lineBrush = new SolidColorBrush(baseColor);
            textBrush = new SolidColorBrush(baseColor);
        }

        private Color GetBaseColor()
        {
            //Colors from http://en.wikipedia.org/wiki/Web_colors
            Random rand = new Random();

            switch (authorsSoFar % 6)
            {
                //Red
                case 0:
                    {
                        switch (rand.Next() % 5)
                        {
                            //IndianRed
                            case 0: return Color.FromArgb(255, 205, 92, 92);

                            //Crimson
                            case 1: return Color.FromArgb(255, 220, 20, 60);

                            //DarkRed
                            case 2: return Color.FromArgb(255, 139, 0, 0);

                            //Pink
                            case 3: return Color.FromArgb(255, 255, 192, 203);

                            //MediumVioletRed
                            case 4: return Color.FromArgb(255, 199, 21, 133);
                        }
                        break;
                    }

                //Yellow
                case 1:
                    {
                        switch (rand.Next() % 5)
                        {
                            //LightSalmon
                            case 0: return Color.FromArgb(255, 255, 160, 120);

                            //Coral
                            case 1: return Color.FromArgb(255, 255, 127, 80);

                            //OrangeRed
                            case 2: return Color.FromArgb(255, 255, 69, 0);

                            //Gold
                            case 3: return Color.FromArgb(255, 255, 215, 0);

                            //DarkKhaki
                            case 4: return Color.FromArgb(255, 189, 183, 107);
                        }
                        break;
                    }

                //Purple
                case 2:
                    {
                        //Lavender
                        switch (rand.Next() % 5)
                        {
                            case 0: return Color.FromArgb(255, 230, 230, 250);

                            //Plum
                            case 1: return Color.FromArgb(255, 221, 160, 221);

                            //Purple
                            case 2: return Color.FromArgb(255, 128, 0, 128);

                            //Indigo
                            case 3: return Color.FromArgb(255, 75, 0, 130);

                            //So technically green but just to make the rands have the same number
                            //GreenYellow
                            case 4: return Color.FromArgb(255, 173, 255, 47);
                        }
                        break;
                    }

                //Green
                case 3:
                    {
                        switch (rand.Next() % 6)
                        {
                            //LineGreen
                            case 0: return Color.FromArgb(255, 50, 205, 50);

                            //SpringGreen
                            case 1: return Color.FromArgb(255, 0, 255, 127);

                            //SeaGreen
                            case 2: return Color.FromArgb(255, 46, 139, 87);

                            //DarkGreen
                            case 3: return Color.FromArgb(255, 0, 100, 0);

                            //DarkOliveGreen
                            case 4: return Color.FromArgb(255, 85, 107, 47);

                            //Teal
                            case 5: return Color.FromArgb(255, 0, 128, 128);
                        }
                        break;
                    }

                //Blue
                case 4:
                    {
                        switch (rand.Next() % 7)
                        {
                            //Aqua, Cyan
                            case 0: return Color.FromArgb(255, 0, 255, 255);

                            //Aquamarine
                            case 1: return Color.FromArgb(255, 127, 255, 212);

                            //CadetBlue
                            case 2: return Color.FromArgb(255, 95, 158, 160);

                            //SteelBlue
                            case 3: return Color.FromArgb(255, 70, 130, 180);

                            //DodgerBlue
                            case 4: return Color.FromArgb(255, 30, 144, 255);

                            //Blue
                            case 5: return Color.FromArgb(255, 0, 0, 255);

                            //MidnightBlue
                            case 6: return Color.FromArgb(255, 25, 25, 112);
                        }
                        break;
                    }
                //Brown
                case 5:
                    {
                        //Cornsilk
                        switch (rand.Next() % 5)
                        {
                            case 0: return Color.FromArgb(255, 255, 248, 220);

                            //RosyBrown
                            case 1: return Color.FromArgb(255, 188, 143, 143);

                            //SandyBrown
                            case 2: return Color.FromArgb(255, 244, 164, 96);

                            //GoldenRod
                            case 3: return Color.FromArgb(255, 218, 165, 32);

                            //SaddleBrown
                            case 4: return Color.FromArgb(255, 139, 69, 19);
                        }
                        break;
                    }
            }

            //This should never be hit but if not here it complains that not all paths return a Color
            return Color.FromArgb(255, 0, 0, 0);
        }
    }
}