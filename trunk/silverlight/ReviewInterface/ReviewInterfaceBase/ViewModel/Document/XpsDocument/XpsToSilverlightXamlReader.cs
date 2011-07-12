//Found online at: http://azharthegreat.codeplex.com/releases/view/44682

/*
 *Microsoft Public License (Ms-PL)

This license governs use of the accompanying software. If you use the software, you accept this license. If you do not accept the license, do not use the software.

1. Definitions

The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under U.S. copyright law.

A "contribution" is the original software, or any additions or changes to the software.

A "contributor" is any person that distributes its contribution under this license.

"Licensed patents" are a contributor's patent claims that read directly on its contribution.

2. Grant of Rights

(A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.

(B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.

3. Conditions and Limitations

(A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.

(B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, your patent license from such contributor to the software ends automatically.

(C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution notices that are present in the software.

(D) If you distribute any portion of the software in source code form, you may do so only under this license by including a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object code form, you may only do so under a license that complies with this license.

(E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Resources;
using System.Xml;

namespace ReviewInterfaceBase.ViewModel.Document.XpsDocument
{
    /// <summary>
    /// Translates "XPS XAML" into "Silverlight XAML" by tweaking the structure
    /// to remove Silverlight-unsupported elements from the source and account
    /// for other similar translation issues. This is a minimal implementation
    /// that supports only the functionality needed to call .ReadOuterXml().
    /// </summary>
    internal class XpsToSilverlightXamlReader : XmlReader
    {
        #region notUsed

        public override int AttributeCount
        {
            get { throw new NotImplementedException(); }
        }

        public override string BaseURI
        {
            get { throw new NotImplementedException(); }
        }

        public override bool EOF
        {
            get { throw new NotImplementedException(); }
        }

        public override string GetAttribute(int i)
        {
            throw new NotImplementedException();
        }

        public override string GetAttribute(string name, string namespaceURI)
        {
            throw new NotImplementedException();
        }

        public override string GetAttribute(string name)
        {
            throw new NotImplementedException();
        }

        public override string LookupNamespace(string prefix)
        {
            throw new NotImplementedException();
        }

        public override bool MoveToAttribute(string name, string ns)
        {
            throw new NotImplementedException();
        }

        public override bool MoveToAttribute(string name)
        {
            throw new NotImplementedException();
        }

        public override XmlNameTable NameTable
        {
            get { throw new NotImplementedException(); }
        }

        public override void ResolveEntity()
        {
            throw new NotImplementedException();
        }

        #endregion notUsed

        protected readonly XmlReader _reader;
        protected readonly Dictionary<string, Stream> _imageSources;
        StreamResourceInfo _streamResourceInfo;
        protected readonly Dictionary<string, string> _elementsToTranslate = new Dictionary<string, string>();
        protected readonly Dictionary<string, List<string>> _elementAttributesToRemove = new Dictionary<string, List<string>>();
        protected readonly List<string> _elementToRemove = new List<string>();
        protected readonly Dictionary<string, List<string>> _elementAttributesToTranslate = new Dictionary<string, List<string>>();
        public readonly List<string> _viewboxOfImages = new List<string>();
        public readonly List<string> _viewPortOfImages = new List<string>();
        protected string _currentElementLocalName;
        protected string _currentAttributeLocalName;
        private int i;

        public XpsToSilverlightXamlReader(XmlReader reader, Dictionary<string, Stream> imageSources, List<string> viewboxOfImages, List<string> viewPortOfImages, StreamResourceInfo streamResourceInfo)
        {
            _reader = reader;
            _imageSources = imageSources;
            _viewboxOfImages = viewboxOfImages;
            _viewPortOfImages = viewPortOfImages;
            _streamResourceInfo = streamResourceInfo;
            _elementsToTranslate.Add("FixedPage", "Canvas");
            _elementsToTranslate.Add("FixedPage.Resources", "Canvas.Resources");
            _elementAttributesToRemove.Add("Canvas", new List<string> { "RenderOptions.EdgeMode" });
            _elementAttributesToRemove.Add("FixedPage", new List<string> { "lang" });
            _elementAttributesToRemove.Add("Glyphs", new List<string> { "BidiLevel" });
            _elementAttributesToRemove.Add("ImageBrush", new List<string> { "ImageSource", "TileMode", "Viewbox", "ViewboxUnits", "Viewport", "ViewportUnits" });
            _elementAttributesToRemove.Add("ResourceDictionary", new List<string> { "Source" });
            _elementAttributesToTranslate.Add("Glyphs", new List<string> { "FontUri" });
            _elementToRemove.Add("ResourceDictionary");
            i = -1;
        }

        public override void Close()
        {
            _reader.Close();
        }

        public override int Depth
        {
            get { return _reader.Depth; }
        }

        public override bool IsEmptyElement
        {
            get { return _reader.IsEmptyElement; }
        }

        public override string LocalName
        {
            get
            {
                var localName = _reader.LocalName;
                // Remember the current element/attribute name
                if (XmlNodeType.Element == _reader.NodeType)
                {
                    _currentElementLocalName = localName;
                }
                else if (XmlNodeType.Attribute == _reader.NodeType)
                {
                    _currentAttributeLocalName = localName;
                }
                // Translate appropriate element names
                if ((XmlNodeType.Element == _reader.NodeType) && _elementsToTranslate.ContainsKey(localName))
                {
                    localName = _elementsToTranslate[localName];
                }
                return localName;
            }
        }

        public override bool MoveToElement()
        {
            return _reader.MoveToElement();
        }

        public override bool MoveToFirstAttribute()
        {
            // Move to the first *valid* attribute (skipping all invalid ones)
            var hadAttributes = _reader.HasAttributes;
            var result = TrackLastAttribute(SkipInvalidAttributes(_reader.MoveToFirstAttribute()));
            if (hadAttributes && !result)
            {
                _reader.MoveToElement();
            }
            return result;
        }

        public override bool MoveToNextAttribute()
        {
            return TrackLastAttribute(SkipInvalidAttributes(_reader.MoveToNextAttribute()));
        }

        protected bool SkipInvalidAttributes(bool result)
        {
            // Skip invalid elements
            while ((result && _elementAttributesToRemove.ContainsKey(_currentElementLocalName) && _elementAttributesToRemove[_currentElementLocalName].Contains(_reader.LocalName)) || _elementToRemove.Contains(_reader.LocalName))
            {
                // Track the ImageBrush/ImageSources for use by SetImageBrushSource
                if (("ImageBrush" == _currentElementLocalName) && ("ImageSource" == _reader.LocalName))
                {
                    i++;
                    var stream = Application.GetResourceStream(_streamResourceInfo, ConvertPartName(_reader.Value)).Stream;
                    _imageSources.Add(_reader.Value + "%" + i, stream);
                }
                else if (("ImageBrush" == _currentElementLocalName) && ("Viewbox" == _reader.LocalName))
                {
                    _viewboxOfImages.Add(_reader.Value);
                }
                else if (("ImageBrush" == _currentElementLocalName) && ("Viewport" == _reader.LocalName))
                {
                    _viewPortOfImages.Add(_reader.Value);
                }
                result = _reader.MoveToNextAttribute();
            }
            return result;
        }

        protected bool TrackLastAttribute(bool result)
        {
            if (!result)
            {
                _currentAttributeLocalName = null;
            }
            return result;
        }

        public override string NamespaceURI
        {
            get { return _reader.NamespaceURI; }
        }

        public override XmlNodeType NodeType
        {
            get { return _reader.NodeType; }
        }

        public override string Prefix
        {
            get { return _reader.Prefix; }
        }

        public override bool Read()
        {
            return _reader.Read();
        }

        public override bool ReadAttributeValue()
        {
            return _reader.ReadAttributeValue();
        }

        public override ReadState ReadState
        {
            get { return _reader.ReadState; }
        }

        internal static Uri ConvertPartName(string partName)
        {
            // Remove the leading '/' from part names since Silverlight doesn't seem to like them there
            return new Uri(partName.TrimStart('/'), UriKind.RelativeOrAbsolute);
        }

        public override string Value
        {
            get
            {
                var value = _reader.Value;
                // Translate the attribute's value to a Uri based off the document Uri
                if (_elementAttributesToTranslate.ContainsKey(_currentElementLocalName) && _elementAttributesToTranslate[_currentElementLocalName].Contains(_currentAttributeLocalName))
                {
                    //string fileName = System.IO.Path.GetFileNameWithoutExtension(_xpsDoc);
                    //string fontUri = string.Format("/{0};component/{1}", fileName + "_font", System.IO.Path.GetFileName(value).Replace(".odttf", ".ttf"));

                    //var stream = Application.GetResourceStream(_streamResourceInfo, ConvertPartName(value)).Stream;
                    ////AssemblyPart asmPart = new AssemblyPart();
                    //asmPart.Load(stream);

                    //App.Current.Resources.Add(System.IO.Path.GetFileName(value), stream);

                    //var abc = App.GetResourceStream(ConvertPartName(value));
                    //var myassembly =  AppDomain.CurrentDomain.DefineDynamicAssembly(new System.Reflection.AssemblyName("DynamicAssembly"), System.Reflection.Emit.AssemblyBuilderAccess.Run);
                    //var module = myassembly.DefineDynamicModule("Fonts");
                    //module.DefineManifestResource(System.IO.Path.GetFileName(value), stream, System.Reflection.ResourceAttributes.Public);

                    //var abc = App.GetResourceStream(ConvertPartName(value));

                    //string fontUri = string.Format("/{0};component/{1}", fileName + "_font", System.IO.Path.GetFileName(value));
                    //value = (new Uri(fontUri, UriKind.Relative)).ToString();
                }
                return value;
            }
        }
    }
}