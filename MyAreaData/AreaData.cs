﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using MyGame;

namespace MyArea.Data
{
	public class AreaData : IAreaData
	{
		Stream m_drawingStream;
		List<SymbolInfo> m_symbolList;
		Materials m_materials;
		Buildings m_buildings; // XXX somewhere else?

		public AreaData()
		{
			ParseSymbols();
			m_materials = new Materials();
			m_buildings = new Buildings();

			var ass = System.Reflection.Assembly.GetExecutingAssembly();
			m_drawingStream = ass.GetManifestResourceStream("MyArea.Data.PlanetCute.xaml");
		}

		public IList<SymbolInfo> Symbols { get { return m_symbolList.AsReadOnly(); } }
		public Materials Materials { get { return m_materials; } }
		public Stream DrawingStream { get { return m_drawingStream; } }
		public Buildings Buildings { get { return m_buildings; } }

		void ParseSymbols()
		{
			var ass = System.Reflection.Assembly.GetExecutingAssembly();
			Stream resStream = ass.GetManifestResourceStream("MyArea.Data.Symbols.xml");

			XDocument root = XDocument.Load(new StreamReader(resStream));
			XElement rootElem = root.Element("Symbols");

			m_symbolList = new List<SymbolInfo>(rootElem.Elements().Count());
			int id = 0;
			foreach (XElement elem in rootElem.Elements())
			{
				var symbol = new SymbolInfo();
				symbol.ID = id++;
				symbol.Name = (string)elem.Element("Name");
				symbol.CharSymbol = ((string)elem.Element("CharSymbol"))[0];
				if (elem.Element("Rotate") != null)
					symbol.Rotation = (double)elem.Element("Rotate");
				if (elem.Element("Drawing") != null)
				{
					var drawingElem = elem.Element("Drawing");
					XAttribute attr;

					attr = drawingElem.Attribute("x");
					if (attr != null)
						symbol.X = (double)attr;

					attr = drawingElem.Attribute("y");
					if (attr != null)
						symbol.Y = (double)attr;

					attr = drawingElem.Attribute("w");
					if (attr != null)
						symbol.Width = (double)attr;

					attr = drawingElem.Attribute("h");
					if (attr != null)
						symbol.Height = (double)attr;

					symbol.DrawingName = (string)drawingElem;
				}
				m_symbolList.Add(symbol);
			}
		}
	}
}
