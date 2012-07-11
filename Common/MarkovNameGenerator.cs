/**
 * From http://www.siliconcommandergames.com/MarkovNameGenerator.htm
 */

using System;
using System.Collections.Generic;

namespace Dwarrowdelf
{
	//Generates random names based on the statistical weight of letter sequences
	//in a collection of sample names
	public class MarkovNameGenerator
	{
		public static readonly string[] OrcNameSamples = {
			"Azog",
			"Balcmeg",
			"Boldog",
			"Bolg",
			"Golfimbul",
			"Gorbag",
			"Gorgol",
			"Grishnákh",
			"Lagduf",
			"Lug",
			"Lugdush",
			"Mauhúr",
			"Muzgash",
			"Orcobal",
			"Othrod",
			"Radbug",
			"Shagrat",
			"Snaga",
			"Ufthak",
			"Uglúk",
		};

		public MarkovNameGenerator(IEnumerable<string> sampleNames, int order, int minLength)
		{
			//fix parameter values
			if (order < 1)
				order = 1;
			if (minLength < 1)
				minLength = 1;

			m_order = order;
			m_minLength = minLength;

			//split comma delimited lines
			foreach (string line in sampleNames)
			{
				string[] tokens = line.Split(',');
				foreach (string word in tokens)
				{
					string upper = word.Trim().ToUpper();
					if (upper.Length < order + 1)
						continue;
					m_samples.Add(upper);
				}
			}

			//Build chains
			foreach (string word in m_samples)
			{
				for (int letter = 0; letter < word.Length - order; letter++)
				{
					string token = word.Substring(letter, order);
					List<char> entry;
					if (m_chains.ContainsKey(token))
						entry = m_chains[token];
					else
					{
						entry = new List<char>();
						m_chains[token] = entry;
					}
					entry.Add(word[letter + order]);
				}
			}
		}

		//Get the next random name
		public string GetNextName()
		{
			//get a random token somewhere in middle of sample word
			string s;

			do
			{
				int n = m_rnd.Next(m_samples.Count);
				int nameLength = m_samples[n].Length;
				s = m_samples[n].Substring(m_rnd.Next(0, m_samples[n].Length - m_order), m_order);
				while (s.Length < nameLength)
				{
					string token = s.Substring(s.Length - m_order, m_order);
					char c = GetLetter(token);
					if (c != '?')
						s += GetLetter(token);
					else
						break;
				}

				if (s.Contains(" "))
				{
					string[] tokens = s.Split(' ');
					s = "";
					for (int t = 0; t < tokens.Length; t++)
					{
						if (tokens[t] == "")
							continue;
						if (tokens[t].Length == 1)
							tokens[t] = tokens[t].ToUpper();
						else
							tokens[t] = tokens[t].Substring(0, 1) + tokens[t].Substring(1).ToLower();
						if (s != "")
							s += " ";
						s += tokens[t];
					}
				}
				else
					s = s.Substring(0, 1) + s.Substring(1).ToLower();
			}
			while (m_used.Contains(s) || s.Length < m_minLength);

			m_used.Add(s);

			return s;
		}

		//Reset the used names
		public void Reset()
		{
			m_used.Clear();
		}

		private Dictionary<string, List<char>> m_chains = new Dictionary<string, List<char>>();
		private List<string> m_samples = new List<string>();
		private List<string> m_used = new List<string>();
		private Random m_rnd = new Random();
		private int m_order;
		private int m_minLength;

		//Get a random letter from the chain
		char GetLetter(string token)
		{
			if (!m_chains.ContainsKey(token))
				return '?';

			List<char> letters = m_chains[token];
			int n = m_rnd.Next(letters.Count);

			return letters[n];
		}
	}
}
