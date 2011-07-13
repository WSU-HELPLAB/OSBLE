using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Windows.Media;
using ReviewInterfaceBase.ViewModel.Document.TextFileDocument.Language;

namespace ReviewInterfaceBase.ViewModel.Document.TextFileDocument
{
    /// <summary>
    /// This is desgined for C and similar languages it assumes that there are predefined keywords, #defines, '' and "" are used in strings and \' escapes the '
    /// </summary>
    public class SyntaxHighlighting
    {
        public event EventHandler ProgressChanged = delegate { };

        private Paragraph paragraph;
        private StreamReader code;
        private bool inComment = false;
        private ILanguage language;
        private Regex keywordRegex;
        private Regex onlyWhiteSpaceRegex;

        public SyntaxHighlighting(StreamReader code, ILanguage language)
        {
            this.code = code;
            this.language = language;
            if (code == null)
            {
                throw new ArgumentNullException("code");
            }
            else if (language == null)
            {
                throw new ArgumentNullException("language");
            }
        }

        public Paragraph Highlight()
        {
            paragraph = new Paragraph();

            onlyWhiteSpaceRegex = new Regex(String.Format(@"^\t*(({0})(\b|<))", String.Join("|", language.WhiteSpaceOnlyBefore)));
            keywordRegex = new Regex(String.Format(@"(\b({0})(\b|<))", String.Join("|", language.KeyWords)));
            string line = code.ReadLine();

            while (line != null)
            {
                inComment = ParseLine(line, inComment);
                line = code.ReadLine();
            }
            return paragraph;
        }

        private void AddTextToRichTextBox(Color color, string line, int start, int end)
        {
            Run run = new Run();
            run.Text = line.Substring(start, end - start);
            run.Foreground = new SolidColorBrush(color);
            paragraph.Inlines.Add(run);
        }

        private void AddNewLineToRichTextBox()
        {
            paragraph.Inlines.Add(new LineBreak());
        }

        private bool ParseLine(string line, bool inComment)
        {
            int i = 0;
            int startofSomething = 0;
            bool insingleQuotes = false;
            bool indoubleQuotes = false;

            Match newMatch = onlyWhiteSpaceRegex.Match(line);
            if (newMatch.Success == true)
            {
                AddTextToRichTextBox(Colors.Blue, line, 0, newMatch.Length);
                i = newMatch.Length;
                startofSomething = i;
            }

            while (i < line.Length)
            {
                if (inComment == false)
                {
                    if (insingleQuotes)
                    {
                        if (line[i] == '\'')
                        {
                            //we gotta watch out for escaped ' because they dont count
                            if (i > 0 && line[i - 1] != '\\')
                            {
                                AddTextToRichTextBox(Colors.Red, line, startofSomething, i + 1);
                                insingleQuotes = false;
                                startofSomething = i + 1;
                            }
                        }
                    }
                    else if (indoubleQuotes)
                    {
                        if (line[i] == '"')
                        {
                            //we gotta watch out for escaped " because they dont count
                            if (i > 0 && line[i - 1] != '\\')
                            {
                                AddTextToRichTextBox(Colors.Red, line, startofSomething, i + 1);
                                indoubleQuotes = false;
                                startofSomething = i + 1;
                            }
                        }
                    }
                    else
                    {
                        //look to see if starting quotes
                        if (line[i] == '"')
                        {
                            indoubleQuotes = true;
                            ParseCode(line, startofSomething, i);
                            startofSomething = i;
                        }
                        else if (line[i] == '\'')
                        {
                            insingleQuotes = true;
                            ParseCode(line, startofSomething, i);
                            startofSomething = i;
                        }
                        else
                        {
                            //not in quotes so look for noteText match
                            foreach (string s in language.InlineComments)
                            {
                                if (i + s.Length < line.Length)
                                {
                                    int j = 0;
                                    while (j < s.Length)
                                    {
                                        if (s[j] == line[i + j])
                                        {
                                            j++;
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }

                                    //if this is true then the inlineComment matched and since inlineComments always go to the end
                                    //of the line I am done with this line
                                    if (j >= s.Length)
                                    {
                                        AddTextToRichTextBox(Colors.Green, line, i, line.Length);
                                        AddNewLineToRichTextBox();
                                        //retrun false because I am not in a multiline noteText just singleline
                                        return false;
                                    }
                                }
                            }
                            //I did not match inline noteText if I get here so did I match multiline start?
                            if (language.StartMultiLineComment != null)
                            {
                                if (i + language.StartMultiLineComment.Length <= line.Length)
                                {
                                    int j = 0;
                                    while (j < language.StartMultiLineComment.Length)
                                    {
                                        if (language.StartMultiLineComment[j] == line[i + j])
                                        {
                                            j++;
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }

                                    //if this is true then the inlineComment matched and since inlineComments always go to the end
                                    //of the line I am done with this line
                                    if (j >= language.StartMultiLineComment.Length)
                                    {
                                        ParseCode(line, startofSomething, i);
                                        //in noteText so set as true
                                        startofSomething = i;
                                        inComment = true;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (language.EndMultiLineComment != null)
                    {
                        if (i + language.EndMultiLineComment.Length <= line.Length)
                        {
                            int j = 0;
                            while (j < language.EndMultiLineComment.Length)
                            {
                                if (language.EndMultiLineComment[j] == line[i + j])
                                {
                                    j++;
                                }
                                else
                                {
                                    break;
                                }
                            }

                            //if this is true then the inlineComment matched and since inlineComments always go to the end
                            //of the line I am done with this line
                            if (j >= language.EndMultiLineComment.Length)
                            {
                                //we can move i ahead (minus one because at the end of the loop gonna add 1)
                                i += language.EndMultiLineComment.Length;
                                AddTextToRichTextBox(Colors.Green, line, startofSomething, i);
                                startofSomething = i + 1;
                                //retrun true because I am in a multiline noteText
                                inComment = false;
                            }
                        }
                    }
                }
                i++;
            }

            if (inComment == true)
            {
                AddTextToRichTextBox(Colors.Green, line, startofSomething, line.Length);
            }

            else if (startofSomething != i)
            {
                ParseCode(line, startofSomething, line.Length);
            }
            AddNewLineToRichTextBox();
            return inComment;
        }

        private void ParseCode(string line, int start, int end)
        {
            Brush blue = new SolidColorBrush(Colors.Blue);
            line = line.Substring(start, end - start);
            MatchCollection allKeyWords = keywordRegex.Matches(line);
            int i = 0;
            foreach (Match m in allKeyWords)
            {
                if (i != m.Index)
                {
                    Run run = new Run();
                    run.Text = line.Substring(i, m.Index - i);
                    paragraph.Inlines.Add(run);
                }
                Run keyword = new Run();
                keyword.Text = m.Value;
                keyword.Foreground = blue;
                paragraph.Inlines.Add(keyword);
                i = m.Index + m.Length;
            }
            if (i != line.Length)
            {
                Run run = new Run();
                run.Text = line.Substring(i, line.Length - i);
                paragraph.Inlines.Add(run);
            }
        }
    }
}