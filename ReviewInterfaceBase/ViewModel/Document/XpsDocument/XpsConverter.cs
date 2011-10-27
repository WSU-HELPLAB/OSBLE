using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Xml;
using Shapes = System.Windows.Shapes;

namespace ReviewInterfaceBase.ViewModel.Document.XpsDocument
{
    public class XpsConverter
    {
        private List<string> pageNames = new List<string>();

        private StreamResourceInfo streamResourceInfo;

        public int NumberOfPages
        {
            get
            {
                return pageNames.Count;
            }
        }

        public void GetPageLocations(Stream xpsStream)
        {
            streamResourceInfo = new StreamResourceInfo(xpsStream, null);

            //first we are going to pull out the .rels file
            StreamResourceInfo rels = Application.GetResourceStream(streamResourceInfo, ConvertStringToUri("/_rels/.rels"));
            string fixedDocSeqPath = "";

            //check to make sure it exists
            if (rels != null)
            {
                //create our reader so we can transverse over the xml easily
                using (XmlReader relsXmlReader = XmlReader.Create(rels.Stream))
                {
                    fixedDocSeqPath = findString(relsXmlReader, "Relationship", "Target", ".fdseq");
                }
            }
            else
            {
                //it might be here
                fixedDocSeqPath = "/FixedDocumentSequence.fdseq";
                if ((Application.GetResourceStream(streamResourceInfo, ConvertStringToUri(fixedDocSeqPath))) == null)
                {
                    //our last guess at where the .fdseq might be
                    fixedDocSeqPath = "/FixedDocSeq.fdseq";
                }
            }
            using (Stream fixedDocStream = Application.GetResourceStream(streamResourceInfo, ConvertStringToUri(fixedDocSeqPath)).Stream)
            {
                using (XmlReader fixedDocReader = XmlReader.Create(fixedDocStream))
                {
                    string fixedDocPath = findString(fixedDocReader, "DocumentReference", "Source", ".fdoc");

                    using (Stream pagesContent = Application.GetResourceStream(streamResourceInfo, ConvertStringToUri(fixedDocPath)).Stream)
                    {
                        using (XmlReader pagesContentReader = XmlReader.Create(pagesContent))
                        {
                            pagesContentReader.ReadToDescendant("PageContent");
                            do
                            {
                                pagesContentReader.MoveToAttribute("Source");

                                //check to see if it is giving the full path or just relative path
                                if (pagesContentReader.Value.Contains(System.IO.Path.GetDirectoryName(fixedDocPath)))
                                {
                                    //ok it is giving the full path
                                    pageNames.Add(pagesContentReader.ReadContentAsString());
                                }
                                else
                                {
                                    //it aint giving the full path it is relative to where we are currently so make it the full path
                                    pageNames.Add(System.IO.Path.Combine(fixedDocPath.Replace(System.IO.Path.GetFileName(fixedDocPath), ""), pagesContentReader.ReadContentAsString()));
                                }
                            } while (pagesContentReader.ReadToNextSibling("PageContent"));
                        }
                    }
                }
            }
        }

        public Canvas GetPage(int pageNumber)
        {
            using (Stream pageStream = Application.GetResourceStream(streamResourceInfo, ConvertStringToUri(pageNames[pageNumber])).Stream)
            {
                using (XmlReader pageReader = XmlReader.Create(pageStream))
                {
                    pageReader.ReadToDescendant("FixedPage");
                    string translatedXaml;
                    Dictionary<string, Stream> imageSources = new Dictionary<string, Stream>();
                    List<string> viewboxOfImages = new List<string>();
                    List<string> viewPortOfImages = new List<string>();
                    using (XpsToSilverlightXamlReader translatingReader = new XpsToSilverlightXamlReader(pageReader, imageSources, viewboxOfImages, viewPortOfImages, streamResourceInfo))
                    {
                        translatedXaml = translatingReader.ReadOuterXml();
                    }

                    //it is no longer xps so got to change the xmlns
                    translatedXaml = translatedXaml.Replace("xmlns=\"http://schemas.microsoft.com/xps/2005/06\"", "xmlns=\"http://schemas.microsoft.com/client/2007\"");
                    translatedXaml = translatedXaml.Replace("FixedPage.NavigateUri", "Tag");
                    Canvas canvas = null;

                    //since all xps docs have a canvas and this spits out a canvas no reason in having a canvas which holds a canvas which holds the documents so instead we get the 'real canvas'
                    canvas = (XamlReader.Load(translatedXaml) as Canvas);

                    List<Panel> panels = new List<Panel>();
                    panels.Add(canvas);

                    GetAllPanels(panels);
                    SetImageBrushes(panels, imageSources);
                    SetGlyphFontSources(panels);

                    //ConvertGlyphsToText(panels);

                    return canvas;
                }
            }
        }

        private void ConvertGlyphsToText(List<Panel> panels)
        {
            var linqCollectionOfGlyphs = from panel in panels
                                         from c in panel.Children
                                         where c is Glyphs
                                         select c as Glyphs;

            List<Glyphs> collectionOfGlyphs = new List<Glyphs>(linqCollectionOfGlyphs);

            List<StackPanel> stackPanels = new List<StackPanel>();

            foreach (Glyphs text in collectionOfGlyphs)
            {
                List<Glyphs> glyphs = new List<Glyphs>();

                while (text.UnicodeString != "")
                {
                    Glyphs glyph = new Glyphs();
                    glyph.UnicodeString = text.UnicodeString[0].ToString();
                    glyph.Indices = text.Indices.Split(new string[] { ";" }, StringSplitOptions.None)[0] + ";";
                    glyph.FontUri = text.FontUri;
                    glyph.FontSource = text.FontSource;
                    glyph.Fill = text.Fill;
                    glyph.OriginX = text.OriginX;
                    glyph.OriginY = text.OriginY;
                    glyph.FontRenderingEmSize = text.FontRenderingEmSize;
                    glyphs.Add(glyph);

                    text.UnicodeString = text.UnicodeString.Remove(0, 1);
                    if (text.Indices.Length != 0)
                    {
                        text.Indices = text.Indices.Remove(0, glyph.Indices.Length);
                    }
                    text.OriginX += glyph.RenderSize.Width;
                }

                Panel parent = (((text as FrameworkElement).Parent) as Panel);
                parent.Children.Remove(text);
                StackPanel sp = new StackPanel();
                sp.Orientation = Orientation.Horizontal;
                sp.SetValue(Canvas.LeftProperty, glyphs[0].OriginX);
                sp.SetValue(Canvas.TopProperty, glyphs[0].OriginY);
                foreach (Glyphs glyph in glyphs)
                {
                    Border br = new Border();
                    br.Child = glyph;
                    glyph.SetValue(Canvas.LeftProperty, 0.0);
                    glyph.SetValue(Canvas.TopProperty, 0.0);
                    glyph.OriginX = 0;
                    glyph.OriginY = 0;
                    glyph.MouseLeftButtonDown += new MouseButtonEventHandler(glyph_MouseLeftButtonDown);
                    sp.Children.Add(br);
                }
                stackPanels.Add(sp);
                parent.Children.Add(sp);
            }
        }

        private void glyph_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ((sender as FrameworkElement).Parent as Border).Background = new SolidColorBrush(Colors.Black);
        }

        private string findString(XmlReader reader, string descendant, string attribute, string lookingFor)
        {
            //find the Relationship descendent
            reader.ReadToDescendant(descendant);
            do
            {
                //we will find the Target Attribute
                reader.MoveToAttribute(attribute);

                //if its target is .fdseq then we have found it
                if (reader.Value.Contains(lookingFor))
                {
                    return reader.ReadContentAsString();

                    //pretty sure i can break out of this...
                }

                //otherwise keep going
            } while (reader.ReadToNextSibling(descendant));
            return "";
        }

        private void SetGlyphFontSources(List<Panel> panels)
        {
            var glyphs = from panel in panels
                         from c in panel.Children
                         where c is Glyphs
                         select c as Glyphs;

            foreach (Glyphs glyph in glyphs)
            {
                Stream fontStream = Application.GetResourceStream(streamResourceInfo, ConvertStringToUri(glyph.FontUri.ToString())).Stream;
                glyph.FontSource = new FontSource(fontStream);
                glyph.SetValue(Canvas.ZIndexProperty, 2);
            }
        }

        private void SetImageBrushes(List<Panel> panels, Dictionary<string, Stream> imageSources)
        {
            List<KeyValuePair<string, Stream>> imageSourceList = imageSources.ToList();

            //we want to get all the children in List<Panel> panels.  But panels have many panel and the panel are the ones with children
            //so using linq we get all the panel in panels and then for all the panel with get all their Children.
            var pathsWithImageBrush = from panel in panels
                                      from c in panel.Children
                                      where c is Shapes.Path
                                      && (c as Shapes.Path).Fill is ImageBrush
                                      select c as Shapes.Path;
            int i = 0;
            foreach (Shapes.Path path in pathsWithImageBrush)
            {
                ImageBrush iBrush = path.Fill as ImageBrush;
                iBrush.Transform = null;

                string source = imageSourceList[i].Key;

                if (".TIF" != Path.GetExtension(source).ToUpper())
                {
                    Stream picStream = imageSources[source];
                    BitmapImage image = new BitmapImage();
                    try
                    {
                        image.SetSource(picStream);
                        iBrush.ImageSource = image;
                    }
                    catch
                    {
                        //UH OHS
                    }
                }

                //path.SetValue(Canvas.TagProperty, imageSourceList[i]);
                i++;
            }
        }

        private void GetAllPanels(List<Panel> panels)
        {
            List<Panel> morePanels = new List<Panel>();
            foreach (Panel panel in panels)
            {
                var p = from c in panel.Children where c is Panel select c as Panel;
                morePanels.AddRange(p);
            }
            if (morePanels.Count != 0)
            {
                GetAllPanels(morePanels);
                panels.AddRange(morePanels);
            }
        }

        private Uri ConvertStringToUri(string str)
        {
            //get ride of any leading /
            return new Uri(str.TrimStart('/'), UriKind.Relative);
        }
    }
}