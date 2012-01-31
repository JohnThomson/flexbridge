using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Chorus.FileTypeHanders;
using Chorus.VcsDrivers.Mercurial;
using Chorus.merge;
using Chorus.merge.xml.generic;
using Palaso.IO;

namespace FLEx_ChorusPlugin.Infrastructure.Handling.Common
{
	internal class ListFileTypeHandlerStrategy : IFieldWorksFileHandler
	{
		private const string CmPossibilityList = "CmPossibilityList";

		#region Implementation of IFieldWorksFileHandler

		public bool CanValidateFile(string pathToFile)
		{
			if (!FileUtils.CheckValidPathname(pathToFile, SharedConstants.List))
				return false;

			return ValidateFile(pathToFile) == null;
		}

		public string ValidateFile(string pathToFile)
		{
			try
			{
				var doc = XDocument.Load(pathToFile);
				var root = doc.Root;
				if (!root.Elements(CmPossibilityList).Any())
					return "Not valid styles file";

				return null;
			}
			catch (Exception e)
			{
				return e.Message;
			}
		}

		public IChangePresenter GetChangePresenter(IChangeReport report, HgRepository repository)
		{
			return FieldWorksChangePresenter.GetCommonChangePresenter(report, repository);
		}

		public IEnumerable<IChangeReport> Find2WayDifferences(FileInRevision parent, FileInRevision child, HgRepository repository)
		{
			return Xml2WayDiffService.ReportDifferences(repository, parent, child,
				null,
				CmPossibilityList, SharedConstants.GuidStr);
		}

		public void Do3WayMerge(MetadataCache mdc, MergeOrder mergeOrder)
		{
			var dir = SharedConstants.Scripture;
			ushort upLevels = 1;

			var path = mergeOrder.pathToOurs;
			if (path.Contains("Linguistics"))
			{
				dir = "Linguistics";
			}

			FieldWorksMergeStrategyServices.AddCustomPropInfo(mdc, mergeOrder, dir, upLevels); // NB: Must be done before FieldWorksCommonMergeStrategy is created.

			XmlMergeService.Do3WayMerge(mergeOrder,
				new FieldWorksCommonMergeStrategy(mergeOrder.MergeSituation, mdc),
				null,
				CmPossibilityList, SharedConstants.GuidStr, WritePreliminaryListInformation);
		}

		public string Extension
		{
			get { return SharedConstants.List; }
		}

		#endregion

		private static void WritePreliminaryListInformation(XmlReader reader, XmlWriter writer)
		{
			reader.MoveToContent();
			writer.WriteStartElement(reader.LocalName);
			reader.Read();
		}
	}
}