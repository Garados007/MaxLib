using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaxLib.Data.HtmlDom
{
    #region Html DOM Parser

    public static class HtmlDomParser
    {
        #region Parsereinstellungen

        public static Encoding DefaultEncoding = Encoding.UTF8;

        #region Parserregeln

        private static List<HtmlDomParserRule> rules =
            new List<HtmlDomParserRule>(new HtmlDomParserRule[]{
                HtmlDomParserRule.BreakElement,
                HtmlDomParserRule.ImgElement,
                HtmlDomParserRule.ScriptElement,
                HtmlDomParserRule.Commentary,
                HtmlDomParserRule.Doctype,
                HtmlDomParserRule.StyleElement,
                HtmlDomParserRule.MetaElement,
                HtmlDomParserRule.LinkElement,
            });
        public static List<HtmlDomParserRule> Rules
        {
            get { return HtmlDomParser.rules; }
        }

        #endregion

        #endregion

        #region ParserMethoden

        public static HtmlDomDocument ParseHtml(string html)
        {
            var doc = new HtmlDomDocument();
            ParseHtml(doc, html);
            return doc;
        }

        public static HtmlDomDocument ParseHtml(System.IO.Stream stream)
        {
            var doc = new HtmlDomDocument();
            ParseHtml(doc, stream);
            return doc;
        }

        public static void ParseHtml(HtmlDomDocument document, string html)
        {
            document.Elements.Clear();
            document.Elements.AddRange(new RealParser
            {
                Document = document,
                Stream = new System.IO.MemoryStream(DefaultEncoding.GetBytes(html))
            }.Build());
        }

        public static void ParseHtml(HtmlDomDocument document, System.IO.Stream stream)
        {
            document.Elements.Clear();
            document.Elements.AddRange(new RealParser
            {
                Document = document,
                Stream = stream
            }.Build());
        }

        #endregion

        #region ParserKlassen - Hier erfolgt das eigentliche Parsing

        class RealParser
        {
            internal HtmlDomDocument Document;
            internal System.IO.Stream Stream;

            public void Test()
            {
                //var l = new List<ParseRaw>();
                //ParseRaw test;
                //do l.Add(test = GetNextSingleRaw());
                //while (test != null);
                //var l = new List<ElementWrapper>();
                //ElementWrapper test;
                //do l.Add(test = GetNextElement());
                //while (test != null);
                var e = Build();
                var html = e.ToList().ConvertAll((hde) => hde.GetOuterHtml());
            }

            public HtmlDomElement[] Build()
            {
                var active = new List<ElementWrapper>();
                ElementWrapper current = null;
                do
                {
                    current = GetNextElement();
                    if (current == null) break;
                    if (current.Type == ElementType.ElementHeader || current.Type == ElementType.TextContent)
                        active.Add(current);
                    if (current.Type == ElementType.HiddenContentByElement)
                    {
                        active[active.Count - 1].Content = current.Content;
                        SetElementContainer(active[active.Count - 1]);
                    }
                    if (current.Type == ElementType.ElementCloser)
                    {
                        var ind = active.FindLastIndex(
                            (ew) => ew.Type == ElementType.ElementHeader && ew.Name == current.Name &&
                                (ew.CloseLevel == 1 || ew.CloseLevel == 2));
                        if (ind == -1)
                        {
                            Document.errors.Add(new HtmlDomParserError()
                                {
                                    Fatal = false,
                                    Line = current.Pos.Item1,
                                    Position = current.Pos.Item2,
                                    Name = "undefined closing tag",
                                    Description = "for this closing tag no opening tags exists",
                                    ErrorText = "</" + current.Name + '>'
                                });
                            continue;
                        }
                        for (int i = ind + 1; i < active.Count; ++i)
                        {
                            bool setted = false;
                            for (int i2 = i - 1; i2 >= ind; --i2)
                                if (active[i2].CloseLevel == 2)
                                {
                                    active[i2].Element.Elements.Add(active[i].Element);
                                    setted = true;
                                    break;
                                }
                            if (!setted) active[ind].Element.Elements.Add(active[i].Element);
                            if (active[i].CloseLevel == 2)
                                Document.errors.Add(new HtmlDomParserError()
                                {
                                    Fatal = false,
                                    Line = active[i].Pos.Item1,
                                    Position = active[i].Pos.Item2,
                                    Name = "no closing tag exists",
                                    Description = "for this tag does not exists a closing tag",
                                    ErrorText = "<" + active[i].Name + " ..."
                                });
                        }
                        active[ind].CloseLevel = 0;
                        if (ind < active.Count - 1) active.RemoveRange(ind + 1, active.Count - ind - 1);
                    }
                }
                while (current != null);
                if (active.Count == 0)
                {
                    Document.errors.Add(new HtmlDomParserError()
                        {
                            Fatal = false,
                            Line = 0,
                            Position = 0,
                            Name = "no content",
                            Description = "this file is empty",
                            ErrorText = ""
                        });
                    return new[] { new HtmlDomElementText() { Text = "" } };
                }
                bool flush = false;
                for (int i = 0; i < active.Count; ++i)
                {
                    if (active[i].CloseLevel == 0 && !flush) continue;
                    flush = true;
                    bool setted = false;
                    for (int i2 = i - 1; i2 >= 0; --i2)
                        if (active[i2].CloseLevel == 2)
                        {
                            active[i2].Element.Elements.Add(active[i].Element);
                            setted = true;
                            break;
                        }
                    if (!setted) active[0].Element.Elements.Add(active[i].Element);
                    if (active[i].CloseLevel == 2)
                        Document.errors.Add(new HtmlDomParserError()
                        {
                            Fatal = false,
                            Line = active[i].Pos.Item1,
                            Position = active[i].Pos.Item2,
                            Name = "no closing tag exists",
                            Description = "for this tag does not exists a closing tag",
                            ErrorText = "<" + active[i].Name + " ..."
                        });
                }
                active.ForEach((ew) => ew.Element.Elements.SetIndentLevel(active[0].Name == null || active[0].Name.ToLower() == "html" ? 0 : 1));
                return active.ConvertAll((ew) => ew.Element).ToArray();
            }

            #region ElementWrapper GetNextElement()

            #region GetNextElement()

            ElementWrapper GetNextElement()
            {
                var ew = new ElementWrapper();
                var sr = GetNextSingleRaw();
                if (sr == null) return null;
                if (sr.Type == ParseRawType.Text && sr.Text == "") return GetNextElement();
                //Texte
                if (sr.Type == ParseRawType.Text)
                {
                    ew.Type = ElementType.TextContent;
                    ew.Element = new HtmlDomElementText() { Text = sr.Text };
                    ew.CloseLevel = 0;
                    ew.Pos = sr.Pos;
                    ew.Name = null;
                    return ew;
                }
                //Hidden
                if (sr.Type == ParseRawType.Hidden)
                {
                    ew.Pos = sr.Pos;
                    ew.CloseLevel = 3;
                    ew.Content = sr.Text;
                    ew.Type = ElementType.HiddenContentByElement;
                    return ew;
                }
                //Error im Parsevorgang
                if (sr.Type == ParseRawType.AttributeBegin||
                    sr.Type==ParseRawType.AttributeEnd||
                    sr.Type==ParseRawType.AttributeName||
                    sr.Type==ParseRawType.AttributeSetter||
                    sr.Type==ParseRawType.AttributeValue||
                    sr.Type==ParseRawType.TagCloser||
                    sr.Type==ParseRawType.TagEnd||
                    sr.Type==ParseRawType.TagName)
                {
                    Document.errors.Add(new HtmlDomParserError()
                        {
                            Fatal = false,
                            Line = sr.Pos.Item1,
                            Position = sr.Pos.Item2,
                            Name = "error in parsing progress",
                            Description = "unexpected data type in current state",
                            ErrorText = sr.Text
                        });
                    return GetNextElement();
                }
                //Elemente
                ew.Pos = sr.Pos;
                sr = GetNextSingleRaw();
                #region Document zuende
                if (sr==null)
                {
                    Document.errors.Add(new HtmlDomParserError()
                    {
                        Fatal = false,
                        Line = ew.Pos.Item1,
                        Position = ew.Pos.Item2,
                        Name = "document ended",
                        Description = "Element expected, but document ended"
                    });
                    return null;
                }
                #endregion
                #region Leerer Tag
                if (sr.Type == ParseRawType.TagEnd)
                {
                    Document.errors.Add(new HtmlDomParserError()
                    {
                        Fatal = false,
                        Line = sr.Pos.Item1,
                        Position = sr.Pos.Item2,
                        Name = "Empty Tag",
                        Description = "An empty Tag found.",
                        ErrorText = sr.Text
                    });
                    return GetNextElement();
                }
                #endregion
                #region Closer Tag
                if (sr.Type == ParseRawType.TagCloser)
                {
                    ew.Type = ElementType.ElementCloser;
                    sr = GetNextSingleRaw();
                    #region Errors
                    if (sr.Type==ParseRawType.TagEnd)
                    {
                        Document.errors.Add(new HtmlDomParserError()
                        {
                            Fatal = false,
                            Line = sr.Pos.Item1,
                            Position = sr.Pos.Item2,
                            Name = "Empty Closing Tag",
                            Description = "An empty closing tag found.",
                            ErrorText = sr.Text
                        });
                        return GetNextElement();
                    }
                    if (sr.Type == ParseRawType.TagCloser)
                    {
                        Document.errors.Add(new HtmlDomParserError()
                        {
                            Fatal = false,
                            Line = sr.Pos.Item1,
                            Position = sr.Pos.Item2,
                            Name = "Double Closing Tag",
                            Description = "multiple closing tags are found",
                            ErrorText = sr.Text
                        });
                    }
                    #endregion
                    ew.Content = ew.Name = sr.Text;
                    sr = GetNextSingleRaw();
                    if (sr.Type != ParseRawType.TagEnd)
                    {
                        Document.errors.Add(new HtmlDomParserError()
                        {
                            Fatal = false,
                            Line = sr.Pos.Item1,
                            Position = sr.Pos.Item2,
                            Name = "arguments found",
                            Description = "some arguments are found in a closing tag",
                            ErrorText = sr.Text
                        });
                        while (sr != null && sr.Type != ParseRawType.TagCloser)
                            sr = GetNextSingleRaw();
                        if (sr == null) Document.errors.Add(new HtmlDomParserError()
                        {
                            Fatal = false,
                            Line = sr.Pos.Item1,
                            Position = sr.Pos.Item2,
                            Name = "document ended",
                            Description = "the document ist ended and the closing tag is not finished"
                        });
                    }
                    ew.CloseLevel = 3;
                    return ew;
                }
                #endregion
                #region Hidden
                else if (sr.Type== ParseRawType.Hidden)
                {
                    ew.CloseLevel = 3;
                    ew.Content = sr.Text;
                    ew.Type = ElementType.HiddenContentByElement;
                    return ew;
                }
                #endregion
                #region Opener Tag
                else
                {
                    ew.Name = sr.Text;
                    ew.Type = ElementType.ElementHeader;
                    var rule = Rules.Find((r) =>
                        (r.Name != null && r.Name.ToLower() == ew.Name.ToLower()) ||
                        (r.StartCode != null && r.StartCode.ToLower() == '<' + ew.Name.ToLower()));
                    if (rule!=null)
                    {
                        if (!rule.NeedEndTag) ew.CloseLevel = 1;
                        if (rule.ElementType != null) ew.Element =
                            Activator.CreateInstance(rule.ElementType, true) as HtmlDomElement;
                        if (rule.DontParseElement)
                        {
                            ew.Content = GetNextSingleRaw().Text;
                            ew.CloseLevel = 0;
                            SetElementContainer(ew);
                            return ew;
                        }
                    }
                    if (ew.Element == null) ew.Element = new HtmlDomElement(ew.Name);
                    else ew.Element.ElementName = ew.Name;
                    #region Attributes
                    sr = GetNextSingleRaw();
                    while (sr != null && sr.Type != ParseRawType.TagCloser && sr.Type != ParseRawType.TagEnd)
                    {
                        var att = new HtmlDomAttribute("", null)
                        {
                            Key = sr.Text
                        };
                        ew.Element.Attributes.Add(att);
                        if ((sr = GetNextSingleRaw()) == null) break;
                        if (sr.Type==ParseRawType.AttributeName) continue;
                        if (sr.Type != ParseRawType.AttributeSetter)
                            Document.errors.Add(new HtmlDomParserError()
                            {
                                Fatal = false,
                                Line = sr.Pos.Item1,
                                Position = sr.Pos.Item2,
                                Name = "missing attribute setter",
                                Description = "no attribute setter (=) found",
                                ErrorText = sr.Text
                            });
                        if ((sr = GetNextSingleRaw()) == null) break;
                        if (sr.Type != ParseRawType.AttributeBegin)
                            Document.errors.Add(new HtmlDomParserError()
                                {
                                    Fatal = false,
                                    Line = sr.Pos.Item1,
                                    Position = sr.Pos.Item2,
                                    Name = "missing value start",
                                    Description = "the attribute value has no start (\")",
                                    ErrorText = sr.Text
                                });
                        else if ((sr = GetNextSingleRaw()) == null) break;
                        att.Value = sr.Type == ParseRawType.AttributeEnd ? "" : sr.Text;
                        if (sr.Type == ParseRawType.AttributeValue)
                            if ((sr = GetNextSingleRaw()) == null) break;
                        if (sr.Type != ParseRawType.AttributeEnd)
                            Document.errors.Add(new HtmlDomParserError()
                            {
                                Fatal = false,
                                Line = sr.Pos.Item1,
                                Position = sr.Pos.Item2,
                                Name = "missing value end",
                                Description = "the attribute value has no end (\")",
                                ErrorText = sr.Text
                            });
                        if (sr.Type == ParseRawType.AttributeEnd) sr = GetNextSingleRaw();
                    }
                    #endregion
                    if (sr == null)
                    {
                        Document.errors.Add(new HtmlDomParserError()
                        {
                            Fatal = false,
                            Line = ew.Pos.Item1,
                            Position = ew.Pos.Item2,
                            Name = "document ended",
                            Description = "the tag is not finish but the document ends",
                            ErrorText = ew.Name
                        });
                        return ew;
                    }
                    if (sr.Type == ParseRawType.TagCloser)
                    {
                        ew.CloseLevel = 0;
                        sr = GetNextSingleRaw();
                        if (sr.Type!= ParseRawType.TagEnd)
                        {
                            Document.errors.Add(new HtmlDomParserError()
                            {
                                Fatal = false,
                                Line = sr.Pos.Item1,
                                Position = sr.Pos.Item2,
                                Name = "missing tag end",
                                Description = "the end of the tag expected but attributes found",
                                ErrorText = sr.Text
                            });
                            while (sr.Type != ParseRawType.TagEnd) sr = GetNextSingleRaw();
                        }
                    }
                    return ew;
                }
                #endregion
            }

            void SetElementContainer(ElementWrapper ew)
            {
                if (ew == null || ew.Element == null) return;
                var type = ew.Element.GetType();
                foreach (var method in type.GetMethods(System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic))
                {
                    var att = method.GetCustomAttributes(typeof(HtmlParserHelperAttribute), true);
                    foreach (HtmlParserHelperAttribute a in att)
                    {
                        if (a.HelperType == HtmlParserHelperType.ContentTarget)
                            method.Invoke(ew.Element, new object[] { ew.Content });
                        if (a.HelperType == HtmlParserHelperType.NameTarget)
                            method.Invoke(ew.Element, new object[] { ew.Name });
                    }
                }
            }

            #endregion

            #region GetNextElement() Helper

            class ElementWrapper
            {
                public HtmlDomElement Element;
                public ElementType Type;
                public int CloseLevel = 2; //2-Closer notwendig; 1-Closer optional; 0-Kein Closer
                public string Content;
                public Tuple<int, int, long> Pos;
                public string Name;

                public override string ToString()
                {
                    return string.Format("{0}: ({1}) = {2} => [{3}]", Type, CloseLevel, Name, Element == null ? "n" : Element.Elements.Count.ToString());
                }
            }
            enum ElementType
            {
                ElementHeader,
                TextContent,
                HiddenContentByElement,
                ElementCloser,
            }

            #endregion

            #endregion

            #region ParseRaw GetNextSingleRaw()

            #region GetNextSingleRaw()

            int Line=1, Position;
            ActiveParserType ActiveType = ActiveParserType.Text;
            bool AttributeStarted = false, nomoreentrys = false, ignorerules = false, SingleQuotedAttribute;
            HtmlDomParserRule activerule = null;

            Queue<ParseRaw> ParseRawQueue = new Queue<ParseRaw>();

            ParseRaw GetNextSingleRaw()
            {
                if (ParseRawQueue.Count == 0)
                {
                    if (nomoreentrys) return null;
                    if (!GetNextRaw()) nomoreentrys = true;
                    if (ParseRawQueue.Count == 0) return null; 
                }
                return ParseRawQueue.Dequeue();
            }
            bool GetNextRaw()
            {
                var pr = new ParseRaw();
                switch (ActiveType)
                {
                    #region Text
                    case ActiveParserType.Text:
                        {
                            pr.Type = ParseRawType.Text;
                            pr.Text = "";
                            pr.Pos = SavePos();
                            var cc = GetNextChar();
                            if (cc == 0) return false;
                            bool jumpspace = false;
                            while (cc != 0 && cc != '<')
                            {
                                if (cc == '\n')
                                {
                                    pr.Text += '\r';
                                    jumpspace = true;
                                }
                                if ((cc != ' ' && cc != '\t') || !jumpspace)
                                {
                                    pr.Text += cc;
                                    jumpspace = false;
                                }
                                cc = GetNextChar();
                            }
                            if (cc == 0)
                            {
                                ParseRawQueue.Enqueue(pr);
                                return false;
                            }
                            ActiveType = ActiveParserType.Tag;
                            pr.Text = pr.Text.Trim();
                            ParseRawQueue.Enqueue(pr);
                            pr = new ParseRaw
                            {
                                Pos = SavePos(),
                                Type = ParseRawType.TagStart,
                                Text = "<"
                            };
                            ParseRawQueue.Enqueue(pr);
                            return true;
                        }
                    #endregion
                    #region Tag
                    case ActiveParserType.Tag:
                        {
                            pr.Pos = SavePos();
                            var cc = GetNextChar();
                            if (cc == 0) return false;
                            pr.Text = "";
                            while (cc != '<' && cc != '/' && cc != '>' && cc != 0 && 
                                (pr.Text == "" || (cc != ' ' && cc != '\n' && cc != '\t')))
                            {
                                if (cc != ' ' || pr.Text != "") pr.Text += cc;
                                if (!ignorerules)
                                {
                                    if ((activerule = rules.Find((r) => r.StartCode != null &&( r.StartCode.ToLower() == '<' + pr.Text.ToLower()))) != null)
                                    {
                                        if (activerule.DontParseContent || activerule.DontParseElement)
                                        {
                                            ActiveType = ActiveParserType.HiddenCode;
                                            break;
                                        }
                                    }
                                }
                                cc = GetNextChar();
                            }
                            if (activerule != null && pr.Text != "")
                            {
                                pr.Type = ParseRawType.TagName;
                                ParseRawQueue.Enqueue(pr);
                                return true;
                            }
                            if (pr.Text != "")
                            {
                                pr.Type = ParseRawType.TagName;
                                while ((cc == ' ' || cc == '\t' || cc == '\n') && cc!=0)
                                    cc = GetNextChar();
                                ActiveType = ActiveParserType.AttributeName;
                                ParseRawQueue.Enqueue(pr);
                                if (cc == 0) return false;
                                if (!ignorerules)
                                {
                                    activerule = rules.Find((r) => r.Name.ToLower() == pr.Text.ToLower());
                                    if (activerule != null)
                                    {
                                        if (activerule.DontParseElement)
                                        {
                                            ActiveType = ActiveParserType.HiddenCode;
                                            GoBack(-1);
                                            return true;
                                        }
                                    }
                                }
                            }
                            if (cc == '<')
                            {
                                pr = new ParseRaw
                                {
                                    Pos = SavePos(),
                                    Type = ParseRawType.TagStart,
                                    Text = "<"
                                };
                                ParseRawQueue.Enqueue(pr);
                                return true;
                            }
                            if (cc == '/')
                            {
                                pr = new ParseRaw
                                {
                                    Pos = SavePos(),
                                    Type = ParseRawType.TagCloser,
                                    Text = "/"
                                };
                                ParseRawQueue.Enqueue(pr);
                                return true;
                            }
                            if (cc == '>')
                            {
                                pr = new ParseRaw
                                {
                                    Pos = SavePos(),
                                    Type = ParseRawType.TagEnd,
                                    Text = ">"
                                };
                                ActiveType = ActiveParserType.Text;
                                ParseRawQueue.Enqueue(pr);
                                ignorerules = false;
                                if (activerule!=null)
                                {
                                    if (activerule.DontParseContent) ActiveType = ActiveParserType.HiddenCode;
                                    else activerule = null;
                                }
                                return true;
                            }
                            GoBack(-1);
                        } break;
                    #endregion
                    #region AttributeName
                    case ActiveParserType.AttributeName:
                        {
                            pr.Pos = SavePos();
                            var cc = GetNextChar();
                            if (cc == 0) return false;
                            pr.Text = "";
                            var emptyjump = true;
                            while (cc != '=' && cc != '"' && cc != '\'' && cc != '>' && (emptyjump||(cc != ' ' && cc != '/' && cc != '\t' && cc != '\n')))
                            {
                                if (cc != ' ' && cc != '/' && cc != '\t' && cc != '\n')
                                {
                                    pr.Text += cc;
                                    emptyjump = false;
                                }
                                cc = GetNextChar();
                                if (cc == 0)
                                {
                                    pr.Type = ParseRawType.AttributeName;
                                    ParseRawQueue.Enqueue(pr);
                                    return false;
                                }
                            }
                            if (pr.Text == "")
                            {
                                if (cc == '/')
                                {
                                    pr.Type = ParseRawType.TagCloser;
                                    pr.Text = "/";
                                    ParseRawQueue.Enqueue(pr);
                                    return true;
                                }
                                if (cc == '=')
                                {
                                    pr.Type = ParseRawType.AttributeSetter;
                                    pr.Text = "=";
                                    ActiveType = ActiveParserType.AttributeValue;
                                    ParseRawQueue.Enqueue(pr);
                                    return true;
                                }
                                if (cc == '"' || cc=='\'')
                                {
                                    pr.Type = ParseRawType.AttributeBegin;
                                    pr.Text = "\"";
                                    ActiveType = ActiveParserType.AttributeValue;
                                    AttributeStarted = true;
                                    SingleQuotedAttribute = cc == '\'';
                                    ParseRawQueue.Enqueue(pr);
                                    return true;
                                }
                                if (cc=='>')
                                {
                                    pr.Type = ParseRawType.TagEnd;
                                    pr.Text = ">";
                                    ActiveType = ActiveParserType.Text;
                                    ParseRawQueue.Enqueue(pr);
                                    if (activerule != null && activerule.DontParseContent)
                                        ActiveType = ActiveParserType.HiddenCode;
                                    else activerule = null;
                                    return true;
                                }
                                return true;
                            }
                            else
                            {
                                pr.Type = ParseRawType.AttributeName;
                                ParseRawQueue.Enqueue(pr);
                                GoBack(-1);
                                return true;
                            }
                        }
                    #endregion
                    #region AttributeValue
                    case ActiveParserType.AttributeValue:
                        {
                            pr.Pos = SavePos();
                            var cc = GetNextChar();
                            if (cc == 0) return false;
                            pr.Text = "";
                            if (AttributeStarted ? SingleQuotedAttribute ? cc == '\'' : cc == '"' : cc == '\'' || cc == '"')
                            {
                                pr.Type = AttributeStarted ? ParseRawType.AttributeEnd : ParseRawType.AttributeBegin;
                                pr.Text = "\"";
                                AttributeStarted = !AttributeStarted;
                                if (!AttributeStarted)
                                {
                                    ActiveType = ActiveParserType.AttributeName;
                                    SingleQuotedAttribute = false;
                                }
                                else SingleQuotedAttribute = cc == '\'';
                                ParseRawQueue.Enqueue(pr);
                                return true;
                            }
                            while ((SingleQuotedAttribute ? cc != '\'' : cc != '"') && cc != 0)
                            {
                                pr.Text += cc;
                                cc = GetNextChar();
                            }
                            pr.Type = ParseRawType.AttributeValue;
                            ParseRawQueue.Enqueue(pr);
                            AttributeStarted = false;
                            ActiveType = ActiveParserType.AttributeName;
                            if (cc == '"' || cc=='\'')
                            {
                                pr = new ParseRaw
                                {
                                    Pos = SavePos(),
                                    Text = "\"",
                                    Type = ParseRawType.AttributeEnd
                                };
                                ParseRawQueue.Enqueue(pr);
                                return true;
                            }
                            else return false;
                        }
                    #endregion
                    #region HiddenCode
                    case ActiveParserType.HiddenCode:
                        {
                            pr.Pos = SavePos();
                            pr.Type = ParseRawType.Hidden;
                            pr.Text = "";
                            var cc = GetNextChar();
                            while (cc != 0)
                            {
                                if (activerule.EndCode!=null)
                                {
                                    pr.Text += cc;
                                    if (pr.Text.EndsWith(activerule.EndCode)) break;
                                }
                                else
                                {
                                    if (cc == '<')
                                    {
                                        var temp = "" + cc;
                                        GoBack(-1);
                                        var pos = SavePos();
                                        GoBack(1);
                                        cc = GetNextChar();
                                        while ((cc == ' ' || cc == '\t' || cc == '\n' || cc == '/') && cc != 0)
                                        {
                                            temp += cc;
                                            cc = GetNextChar();
                                        }
                                        if (cc == 0)
                                        {
                                            pr.Text += temp;
                                            break;
                                        }
                                        var temp2 = ""+cc;
                                        for (int i = 1; i < activerule.Name.Length; ++i)
                                        {
                                            if (!activerule.Name.StartsWith(temp2, StringComparison.InvariantCultureIgnoreCase))
                                                break;
                                            cc = GetNextChar();
                                            if (cc == 0) break;
                                            temp2 += cc;
                                        }
                                        if (cc == 0)
                                        {
                                            pr.Text += temp2;
                                            break;
                                        }
                                        if (temp2.ToLower() == activerule.Name.ToLower())
                                        {
                                            RestorePos(pos);
                                            break;
                                        }
                                        else
                                        {
                                            pr.Text += temp + temp2;
                                        }
                                    }
                                    else pr.Text += cc;
                                }
                                cc = GetNextChar();
                            }
                            if (cc == 0)
                            {
                                ParseRawQueue.Enqueue(pr);
                                return false;
                            }
                            if (activerule.EndCode!=null)
                            {
                                pr.Text = pr.Text.Remove(pr.Text.Length - activerule.EndCode.Length);
                                ParseRawQueue.Enqueue(pr);
                                ActiveType = ActiveParserType.Text;
                            }
                            else
                            {
                                ParseRawQueue.Enqueue(pr);
                                ignorerules = true;
                                ActiveType = ActiveParserType.Tag;
                            }
                            pr.Text = pr.Text.Trim();
                            activerule = null;
                            return true;
                        }
                    #endregion
                }
                return true;
            }

            Tuple<int,int,long> SavePos()
            {
                return new Tuple<int, int, long>(Line, Position, Stream.Position);
            }
            void RestorePos(Tuple<int,int,long> pos)
            {
                Line = pos.Item1;
                Position = pos.Item2;
                Stream.Position = pos.Item3;
            }

            void GoBack(int pos)
            {
                Stream.Position += pos;
                Position += pos;
            }

            char GetNextChar()
            {
                if (Stream.Position == Stream.Length) return (char)0;
                var buf = new byte[DefaultEncoding.GetMaxByteCount(1)];
                var length = 1;
                Stream.Read(buf, 0, length);
                if (DefaultEncoding.WebName=="utf-8") //UTF-8 FIX
                {
                    const byte mask2byte = 0xC0;
                    const byte mask3byte = 0xE0;
                    const byte mask4byte = 0xF0;
                    const byte mask5byte = 0xF8;
                    const byte mask6byte = 0xFC;
                    if ((buf[0] & mask6byte) == mask6byte)
                    {
                        Stream.Read(buf, 1, 5);
                        length += 5;
                    }
                    else if ((buf[0] & mask5byte) == mask5byte)
                    {
                        Stream.Read(buf, 1, 4);
                        length += 4;
                    }
                    else if ((buf[0] & mask4byte) == mask4byte)
                    {
                        Stream.Read(buf, 1, 3);
                        length += 3;
                    }
                    else if ((buf[0] & mask3byte) == mask3byte)
                    {
                        Stream.Read(buf, 1, 2);
                        length += 2;
                    }
                    else if ((buf[0] & mask2byte) == mask2byte)
                    {
                        Stream.Read(buf, 1, 1);
                        length++;
                    }
                    //if (buf[0]==195)
                    //{
                    //    Stream.Read(buf, buf.Length - 1, 1);
                    //    length++;
                    //}
                }
                var s = DefaultEncoding.GetString(buf, 0, length); //195,182;;195,188;195,159
                if (s=="\n")
                {
                    Line++;
                    Position = 0;
                }
                if (s == "\r") 
                {
                    Stream.Read(buf, 0, buf.Length);
                    s = DefaultEncoding.GetString(buf);
                    if (s != "\n")
                    {
                        Stream.Position--;
                        s = "\n";
                    }
                    Line++;
                    Position = 0;
                }
                Position++;
                return s[0];
            }

            #endregion

            #region GetNextSingleRaw() Helper

            class ParseRaw
            {
                internal ParseRawType Type;
                internal string Text;
                internal Tuple<int, int, long> Pos;

                public override string ToString()
                {
                    return string.Format("{0} {1}: {2}", Pos, Type, Text);
                }
            }
            enum ParseRawType
            {
                TagStart,       //Start eines Tags <
                TagEnd,         //Ende eines Tags >
                TagCloser,      //Markierer für das Schließen eines Tags /
                TagName,        //Der Name des Tags
                AttributeName,  //Der Name eines Attributs
                AttributeSetter,//Das = Zeichen für ein Attribut
                AttributeBegin, //Der Start des Attributwerts "
                AttributeEnd,   //Das Ende eines Attributwerts "
                AttributeValue, //Der Wert eines Attributs
                Text,           //Ein einfacher Text
                Hidden,         //Inhalte, die nicht geparst werden durften
            }
            enum ActiveParserType
            {
                Text,
                Tag,
                AttributeName,
                AttributeValue,
                HiddenCode
            }

            #endregion

            #endregion
        }

        #endregion
    }

    #region Zusatzinformationen zum Html DOM Parser

    public class HtmlDomParserRule
    {
        public string Name { get; set; }
        public string StartCode { get; set; }
        public string EndCode { get; set; }
        public bool DontParseContent { get; set; }
        public bool DontParseElement { get; set; }
        public bool NeedEndTag { get; set; }
        public Type ElementType { get; set; }

        public HtmlDomParserRule()
        {
            Name = "";
            StartCode = EndCode = null;
            DontParseContent = DontParseElement = false;
            NeedEndTag = true;
            ElementType = null;
        }

        public static HtmlDomParserRule BreakElement
        { get { return new HtmlDomParserRule() { Name = "br", NeedEndTag = false }; } }

        public static HtmlDomParserRule ImgElement
        { get { return new HtmlDomParserRule() { Name = "img", NeedEndTag = false, ElementType = typeof(HtmlDomElementImg) }; } }

        public static HtmlDomParserRule ScriptElement
        {
            get { return new HtmlDomParserRule() { Name = "script", DontParseContent = true, ElementType = typeof(HtmlDomElementScript) }; }
        }

        public static HtmlDomParserRule Commentary
        { get { return new HtmlDomParserRule() { StartCode = "<!--", EndCode="-->", DontParseContent=true, DontParseElement = true, NeedEndTag = false, ElementType = typeof(HtmlDomElementComment) }; } }

        public static HtmlDomParserRule Doctype
        { get { return new HtmlDomParserRule() { StartCode = "<!DOCTYPE", EndCode = ">", DontParseContent = true, DontParseElement = true, NeedEndTag = false, ElementType = typeof(HtmlDomElementDoctype) }; } }

        public static HtmlDomParserRule StyleElement
        {
            get { return new HtmlDomParserRule() { Name = "style", DontParseContent = true, ElementType = typeof(HtmlDomElementScript) }; }
        }

        public static HtmlDomParserRule MetaElement
        { get { return new HtmlDomParserRule() { Name = "meta", NeedEndTag = false }; } }

        public static HtmlDomParserRule LinkElement
        { get { return new HtmlDomParserRule() { Name = "link", NeedEndTag = false }; } }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public sealed class HtmlParserHelperAttribute : Attribute
    {
        public HtmlParserHelperAttribute(HtmlParserHelperType helperType)
        {
            HelperType = helperType;
        }

        public HtmlParserHelperType HelperType { get; set; }
    }

    public enum HtmlParserHelperType
    {
        ContentTarget,
        NameTarget
    }

    public class HtmlDomParserError
    {
        public bool Fatal { get; internal set; }
        public string Name { get; internal set; }
        public string Description { get; internal set; }
        public string FatalCode { get; internal set; }
        public int Line { get; internal set; }
        public int Position { get; internal set; }
        public string ErrorText { get; internal set; }

        public override string ToString()
        {
            return string.Format("[{0}] ({1},{2}) {3} - '{4}' in '{5}' > '{6}'",
                Fatal ? "FATAL" : "ERROR", Line, Position, Name, Description, ErrorText, FatalCode);
        }
    }

    #endregion

    #endregion

    #region Alle DOM Elemente

    #region Basis Element

    public class HtmlDomBasicElement
    {
        /// <summary>
        /// Setzt oder ruft den inneren Html-Code ab.
        /// </summary>
        public string Html
        {
            get
            {
                return GetHtml();
            }
            set
            {
                var doc = HtmlDomParser.ParseHtml(value);
                Elements.Clear();
                foreach (var e in doc.Elements) Elements.Add(e);
                Elements.AddIndentLevel(IndentLevel + 1);
                errors.Clear();
                errors.AddRange(doc.Errors);
            }
        }

        protected virtual string GetHtml()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < Elements.Count; ++i)
            {
                var e = Elements[i];
                if (e == null) continue;
                sb.Append(e.GetOuterHtml());
            }
            return sb.ToString();
        }
        
        public HtmlDomElementCollection Elements { get; private set; }

        public HtmlDomBasicElement()
        {
            Elements = new HtmlDomElementCollection();
        }

        public int IndentLevel { get; set; }

        public virtual HtmlDomDocument ToDocument()
        {
            var doc = new HtmlDomDocument();
            doc.Elements.AddRange(Elements);
            return doc;
        }

        internal List<HtmlDomParserError> errors = new List<HtmlDomParserError>();
        public HtmlDomParserError[] Errors
        {
            get { return errors.ToArray(); }
        }
    }

    #endregion

    #region Html Dom Elemente

    public class HtmlDomElement : HtmlDomBasicElement, ICloneable
    {
        #region Kontruktoren

        public HtmlDomElement()
        {
            Attributes = new List<HtmlDomAttribute>();
            Class = new HtmlDomClass(this);
            base.Elements.Parent = this;
        }
        public HtmlDomElement(string name) : this()
        {
            ElementName = name;
        }
        public HtmlDomElement(string name, params HtmlDomAttribute[] attributes) : this(name)
        {
            Attributes.AddRange(attributes);
        }

        #endregion

        #region Eigenschaften des HTML DOM Elements

        public string ElementName { get; set; }

        #region Attribute

        public List<HtmlDomAttribute> Attributes { get; private set; }
        public HtmlDomAttribute[] GetAttribute(string key, bool createAttribute = false)
        {
            var array = Attributes.FindAll((a) => a.Key.ToLower() == key.ToLower()).ToArray();
            if (createAttribute && array.Length == 0) 
                array = new[] { new HtmlDomAttribute(key, "") };
            return array;
        }
        public void SetAttribute(string key, object value, bool append = false)
        {
            if (append) Attributes.Add(new HtmlDomAttribute(key, value));
            else
            {
                var al = GetAttribute(key);
                if (al.Length == 0) Attributes.Add(new HtmlDomAttribute(key, value));
                else al[0].Value = value.ToString();
            }
        }

        #endregion

        public string Id
        {
            get
            {
                return GetAttribute("id", true)[0].Value;
            }
            set
            {
                SetAttribute("id", value);
            }
        }

        public HtmlDomClass Class { get; private set; }

        #endregion

        #region Methoden für die HTML Ausgabe

        /// <summary>
        /// Wenn true, wird in der HTML-Ausgabe des Tags nicht verkürzt dargestellt, wenn dieser leer ist. 
        /// Aktiviert: &lt;br&gt;&lt;/br&gt; Nicht aktiviert: &lt;br/&gt;
        /// </summary>
        protected bool ClosingTagRequired = false;

        public virtual string GetOuterHtml()
        {
            var sb = new StringBuilder();
            sb.Append(GetStartTag());
            if (Elements.Count > 0 || ClosingTagRequired)
            {
                sb.Append(Html);
                sb.Append(GetEndTag());
            }
            return sb.ToString();
        }

        protected virtual string GetStartTag()
        {
            var sb = new StringBuilder();
            sb.Append(' ', 2 * IndentLevel);
            sb.Append("<");
            sb.Append(ElementName);
            for (int i = 0; i < Attributes.Count; ++i)
            {
                var a = Attributes[i];
                sb.Append(" ");
                sb.Append(a.Key);
                sb.Append("=\"");
                sb.Append(a.Value);
                sb.Append("\"");
            }
            if (Elements.Count == 0 && !ClosingTagRequired) sb.Append(" /");
            sb.AppendLine(">");
            return sb.ToString();
        }

        protected virtual string GetEndTag()
        {
            var sb = new StringBuilder();
            if (Elements.Count > 0 || ClosingTagRequired)
            {
                sb.Append(' ', 2 * IndentLevel);
                sb.Append("</");
                sb.Append(ElementName);
                sb.AppendLine(">");
            }
            return sb.ToString();
        }

        #endregion

        #region Auflistungsmethoden mit Eltern, Kind oder Nachbarelementen

        public HtmlDomElement Parent { get; internal set; }

        public bool RemoveFromParent()
        {
            if (Parent == null) return false;
            var res = Parent.Elements.Remove(this);
            Parent = null;
            return res;
        }

        public void AddToParent(HtmlDomElement parent)
        {
            if (parent == null) throw new ArgumentNullException("parent");
            Parent.Elements.Add(this);
        }

        public bool InsertAfter(HtmlDomElement element)
        {
            if (element == null) throw new ArgumentNullException("element");
            if (element.Parent == null) return false;
            element.Parent.Elements.Insert(element.Parent.Elements.IndexOf(element) + 1, this);
            return true;
        }

        public bool InsertBefore(HtmlDomElement element)
        {
            if (element == null) throw new ArgumentNullException("element");
            if (element.Parent == null) return false;
            element.Parent.Elements.Insert(element.Parent.Elements.IndexOf(element), this);
            return true;
        }

        #endregion

        #region Von ICloneable geerbt

        public virtual HtmlDomElement Clone()
        {
            return (HtmlDomElement)((ICloneable)this).Clone();
        }

        object ICloneable.Clone()
        {
            var e = (HtmlDomElement)Activator.CreateInstance(GetType(), true);
            e.ClosingTagRequired = ClosingTagRequired;
            e.ElementName = ElementName;
            e.IndentLevel = IndentLevel;
            e.Attributes.AddRange(Attributes.ConvertAll((a) => a.Clone()));
            e.Elements.AddRange(Elements.ConvertAll((el) => el.Clone()));
            return e;
        }

        #endregion
    }

    #region Erweiterte Dom Elemente

    public class HtmlDomElementImg : HtmlDomElement
    {
        public string Src
        {
            get { return GetAttribute("src", true)[0].Value; }
            set { SetAttribute("src", value); }
        }
    }

    public class HtmlDomElementScript : HtmlDomElement
    {
        public HtmlDomElementScript() : base()
        {
            ClosingTagRequired = true;
        }

        public string ScriptCode { get; set; }

        [HtmlParserHelper(HtmlParserHelperType.ContentTarget)]
        protected void SetScriptCode(string code)
        {
            ScriptCode = code;
        }

        public override string GetOuterHtml()
        {
            var sb = new StringBuilder();
            sb.Append(GetStartTag());
            var lines = (ScriptCode ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; ++i)
            {
                sb.Append(' ', 2 + 2 * (IndentLevel));
                sb.AppendLine(lines[i]);
            }
            sb.Append(GetEndTag());
            return sb.ToString();
        }
    }

    public class HtmlDomElementStyle : HtmlDomElement
    {
        public string StyleCode { get; set; }

        [HtmlParserHelper(HtmlParserHelperType.ContentTarget)]
        protected void SetStyleCode(string code)
        {
            StyleCode = code;
        }

        public override string GetOuterHtml()
        {
            var sb = new StringBuilder();
            sb.Append(GetStartTag());
            var lines = (StyleCode ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; ++i)
            {
                sb.Append(' ', 2 + 2 * (IndentLevel));
                sb.AppendLine(lines[i]);
            }
            sb.Append(GetEndTag());
            return sb.ToString();
        }
    }

    public class HtmlDomElementText : HtmlDomElement
    {
        public string Text { get; set; }

        public override string GetOuterHtml()
        {
            var sb = new StringBuilder();
            var lines = Text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; ++i)
            {
                sb.Append(' ', 2 * IndentLevel);
                sb.AppendLine(lines[i].Trim());
            }
            return sb.ToString();
        }
    }

    public class HtmlDomElementComment : HtmlDomElement
    {
        public string Comment { get; set; }

        [HtmlParserHelper(HtmlParserHelperType.ContentTarget)]
        protected void SetComment(string comment)
        {
            Comment = comment;
        }

        public override string GetOuterHtml()
        {
            var sb = new StringBuilder();
            sb.Append(' ', 2 * IndentLevel);
            sb.Append("<!--");
            var lines = (Comment ?? "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; ++i)
            {
                if (i != 0) sb.Append(' ', 2 * IndentLevel);
                sb.Append(lines[i].Trim());
                if (i != lines.Length - 1) sb.AppendLine();
            }
            sb.AppendLine("-->");
            return sb.ToString();
        }
    }

    public class HtmlDomElementDoctype : HtmlDomElement
    {
        public string Doctype { get; set; }

        [HtmlParserHelper(HtmlParserHelperType.ContentTarget)]
        protected void SetDoctype(string doctype)
        {
            Doctype = doctype;
        }

        public override string GetOuterHtml()
        {
            var sb = new StringBuilder();
            sb.Append(' ', 2 * IndentLevel);
            sb.Append("<!DOCTYPE");
            sb.Append(Doctype);
            sb.AppendLine(">");
            return sb.ToString();
        }
    }

    #endregion

    #endregion

    #region Eigenschaften der DOM Elemente

    public class HtmlDomAttribute : ICloneable
    {
        public string Key { get; set; }
        public string Value { get; set; }

        public HtmlDomAttribute(string key, object value)
        {
            Key = key;
            Value = value == null ? "" : value.ToString();
        }

        public override string ToString()
        {
            if (Value == null) return Key;
            else return string.Format("{0}=\"{1}\"", Key, Value);
        }

        #region Von ICloneable geerbt

        public HtmlDomAttribute Clone()
        {
            return new HtmlDomAttribute(Key, Value);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        #endregion
    }

    public class HtmlDomClass : IList<string>
    {
        public HtmlDomClass(HtmlDomElement element)
        {
            Element = element;
        }

        public HtmlDomElement Element { get; private set; }

        List<string> GetTiles()
        {
            var att = Element.GetAttribute("class");
            if (att.Length == 0) return new List<string>();
            var rawtiles = att[0].Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var tiles = new List<string>();
            for (int i = 0; i < rawtiles.Length; ++i) if (!tiles.Contains(rawtiles[i])) tiles.Add(rawtiles[i]);
            return tiles;
        }

        void SetTiles(List<string> tiles)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < tiles.Count; ++i)
            {
                if (i != 0) sb.Append(' ');
                sb.Append(tiles[i]);
            }
            Element.SetAttribute("class", sb);
        }

        public int IndexOf(string item)
        {
            return GetTiles().IndexOf(item);
        }

        public void Insert(int index, string item)
        {
            var list = GetTiles();
            list.Insert(index, item);
            SetTiles(list);
        }

        public void RemoveAt(int index)
        {
            var list = GetTiles();
            list.RemoveAt(index);
            SetTiles(list);
        }

        public string this[int index]
        {
            get
            {
                return GetTiles()[index];
            }
            set
            {
                var list = GetTiles();
                list[index] = value;
                SetTiles(list);
            }
        }

        public void Add(string item)
        {
            var list = GetTiles();
            list.Add(item);
            SetTiles(list);
        }

        public void Clear()
        {
            SetTiles(new List<string>());
        }

        public bool Contains(string item)
        {
            return GetTiles().Contains(item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            GetTiles().CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return GetTiles().Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(string item)
        {
            var list = GetTiles();
            var removed = list.Remove(item);
            SetTiles(list);
            return removed;
        }

        public IEnumerator<string> GetEnumerator()
        {
            return GetTiles().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetTiles().GetEnumerator();
        }
    }

    public class HtmlDomElementCollection : IList<HtmlDomElement>
    {
        #region Datenquelle

        List<HtmlDomElement> elements = new List<HtmlDomElement>();

        internal HtmlDomElement Parent;

        #endregion

        #region Erweiterungen

        #region Bestimmt die Einschübe des Codes, wenn dieser formatiert ausgegeben wird

        public void AddIndentLevel(int level)
        {
            elements.ForEach((e) =>
            {
                if (e != null)
                {
                    e.IndentLevel += level;
                    e.Elements.SetIndentLevel(level);
                }
            });
        }

        public void SetIndentLevel(int level)
        {
            elements.ForEach((e) =>
            {
                if (e != null)
                {
                    e.IndentLevel = level;
                    e.Elements.SetIndentLevel(level+1);
                }
            });
        }

        #endregion

        #region Getter: Sucht bestimmte Elemente anhand des Names, Id oder Klasse heraus

        public HtmlDomElement GetElementById(string id)
        {
            for (int i = 0; i<elements.Count; ++i)
            {
                var e = elements[i];
                if (e.Id == id) return e;
                e = e.Elements.GetElementById(id);
                if (e != null) return e;
            }
            return null;
        }

        public HtmlDomElement[] GetElementsByName(string name)
        {
            var l = new List<HtmlDomElement>();
            for (int i = 0; i<elements.Count; ++i)
            {
                var e = elements[i];
                if (e.ElementName == name) l.Add(e);
                l.AddRange(e.Elements.GetElementsByName(name));
            }
            return l.ToArray();
        }

        public HtmlDomElement[] GetElementsByClassName(string name)
        {
            var l = new List<HtmlDomElement>();
            for (int i = 0; i<elements.Count; ++i)
            {
                var e = elements[i];
                if (e.Class.Contains(name)) l.Add(e);
                l.AddRange(e.Elements.GetElementsByClassName(name));
            }
            return l.ToArray();
        }

        #endregion

        #region Spezielle Funktionen von List<>

        public List<T> ConvertAll<T>(Converter<HtmlDomElement,T> converter)
        {
            return elements.ConvertAll(converter);
        }

        #endregion

        #endregion

        #region Erbschaft von IList

        public int IndexOf(HtmlDomElement item)
        {
            return elements.IndexOf(item);
        }

        public void Insert(int index, HtmlDomElement item)
        {
            elements.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            elements[index].Parent = null;
            elements.RemoveAt(index);
        }

        public HtmlDomElement this[int index]
        {
            get
            {
                return elements[index];
            }
            set
            {
                elements[index] = value;
            }
        }

        public void Add(HtmlDomElement item)
        {
            elements.Add(item);
            item.Parent = Parent;
        }

        public void AddRange(IEnumerable<HtmlDomElement> collection)
        {
            elements.AddRange(collection);
            foreach (var e in collection) e.Parent = Parent;
        }

        public void Clear()
        {
            elements.ForEach((e) => e.Parent = null);
            elements.Clear();
        }

        public bool Contains(HtmlDomElement item)
        {
            return elements.Contains(item);
        }

        public void CopyTo(HtmlDomElement[] array, int arrayIndex)
        {
            elements.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return elements.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(HtmlDomElement item)
        {
            item.Parent = null;
            return elements.Remove(item);
        }

        public IEnumerator<HtmlDomElement> GetEnumerator()
        {
            return elements.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return elements.GetEnumerator();
        }

        #endregion
    }

    #endregion

    #region DOM Dokument

    public class HtmlDomDocument : HtmlDomBasicElement
    {
        public virtual HtmlDomDataSource ToDataSource()
        {
            return new HtmlDomDataSource(this);
        }

        public override HtmlDomDocument ToDocument()
        {
            return this;
        }
    }

    #endregion

    #endregion

    #region Integration des Html DOM Models in den Webserver

    public class HtmlDomDataSource : Net.Webserver.HttpDataSource
    {
        public HtmlDomDocument Document { get; private set; }

        public HtmlDomDataSource(HtmlDomDocument document)
        {
            Document = document;
        }

        public override void Dispose()
        {
        }

        public override long AproximateLength()
        {
            return HtmlDomParser.DefaultEncoding.GetByteCount(Document.Html);
        }

        public override long WriteToStream(System.IO.Stream networkStream)
        {
            var b = HtmlDomParser.DefaultEncoding.GetBytes(Document.Html);
            networkStream.Write(b, 0, b.Length);
            return b.LongLength;
        }

        public override long ReadFromStream(System.IO.Stream networkStream, long readlength)
        {
            var b = new byte[readlength];
            networkStream.Read(b, 0, b.Length);
            Document.Html = HtmlDomParser.DefaultEncoding.GetString(b);
            return readlength;
        }

        public override byte[] GetSourcePart(long start, long length)
        {
            var b = HtmlDomParser.DefaultEncoding.GetBytes(Document.Html).ToList();
            return b.GetRange((int)start, (int)length).ToArray();
        }

        public override int WriteSourcePart(byte[] source, long start, long length)
        {
            var b = HtmlDomParser.DefaultEncoding.GetBytes(Document.Html).ToList();
            int i = 0;
            for (; i < length; ++i)
                if (i + start >= b.Count) b.Add(source[i]);
                else b[(int)start + i] = source[i];
            Document.Html = HtmlDomParser.DefaultEncoding.GetString(b.ToArray());
            return i;
        }

        public override long ReserveExtraMemory(long bytes)
        {
            return 0;
        }
    }

    #endregion
}
