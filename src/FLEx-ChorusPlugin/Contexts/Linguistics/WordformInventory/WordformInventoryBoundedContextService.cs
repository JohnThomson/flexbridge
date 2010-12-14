﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using FLEx_ChorusPlugin.Infrastructure;
using FLEx_ChorusPlugin.Infrastructure.DomainServices;

namespace FLEx_ChorusPlugin.Contexts.Linguistics.WordformInventory
{
	internal static class WordformInventoryBoundedContextService
	{
		internal static void NestContext(string linguisticsBaseDir, IDictionary<string,
			SortedDictionary<string, string>> classData,
			Dictionary<string, string> guidToClassMapping)
		{
			var sortedPunctuationFormInstanceData = classData["PunctuationForm"];
			var sortedWfiWordformInstanceData = classData["WfiWordform"];

			var inventoryDir = Path.Combine(linguisticsBaseDir, SharedConstants.WordformInventoryRootFolder);
			if (!Directory.Exists(inventoryDir))
				Directory.CreateDirectory(inventoryDir);

			// the doc root will be "Inventory" (SharedConstants.WordformInventoryRootFolder).
			// This will store the PunctuationForm instances (unowned) in the header, and each PunctuationForm will be a child of header.
			// Each WfiWordform (unowned) will then be a child of root.
			var header = new XElement(SharedConstants.Header);
			// Work on copy, since 'classData' is changed during the loop.
			SortedDictionary<string, string> srcDataCopy;
			if (sortedPunctuationFormInstanceData.Count > 0)
			{
				// There may be no punct forms, even if there are wordforms, so header really is optional.
				srcDataCopy = new SortedDictionary<string, string>(sortedPunctuationFormInstanceData);
				foreach (var punctFormStringData in srcDataCopy.Values)
				{
					var pfElement = XElement.Parse(punctFormStringData);
					header.Add(pfElement);
					CmObjectNestingService.NestObject(false,
						pfElement,
						classData,
						guidToClassMapping);
				}
			}

			var nestedData = new SortedDictionary<string, string>();
			if (sortedWfiWordformInstanceData.Count > 0)
			{
				// Work on copy, since 'classData' is changed during the loop.
				srcDataCopy = new SortedDictionary<string, string>(sortedWfiWordformInstanceData);
				foreach (var wordFormElement in srcDataCopy.Values)
				{
					var wfElement = XElement.Parse(wordFormElement);
					var checksumProperty = wfElement.Element("Checksum");
					if (checksumProperty != null)
						checksumProperty.Remove();
					CmObjectNestingService.NestObject(false,
													  wfElement,
													  classData,
													  guidToClassMapping);
					nestedData.Add(wfElement.Attribute(SharedConstants.GuidStr).Value.ToLowerInvariant(), wfElement.ToString());
				}
			}

			var buckets = FileWriterService.CreateEmptyBuckets(10);
			FileWriterService.FillBuckets(buckets, nestedData);

			for (var i = 0; i < buckets.Count; ++i )
			{
				var root = new XElement(SharedConstants.WordformInventoryRootFolder);
				if (i == 0 && header.HasElements)
					root.Add(header);
				var currentBucket = buckets[i];
				foreach (var wordformString in currentBucket.Values)
				{
					var wordformElement = XElement.Parse(wordformString);
					root.Add(wordformElement);
				}
				FileWriterService.WriteNestedFile(PathnameForBucket(inventoryDir, i), root);
			}
		}

		internal static string PathnameForBucket(string inventoryDir, int bucket)
		{
			return Path.Combine(inventoryDir, string.Format("{0}_{1}{2}.{3}", SharedConstants.WordformInventory, bucket >= 9 ? "" : "0", bucket + 1, SharedConstants.Inventory));
		}

		internal static void FlattenContext(
			SortedDictionary<string, XElement> highLevelData,
			SortedDictionary<string, XElement> sortedData,
			string linguisticsBaseDir)
		{
			// There is only one file here: Path.Combine(inventoryDir, SharedConstants.WordformInventoryFilename)
			var inventoryDir = Path.Combine(linguisticsBaseDir, SharedConstants.WordformInventoryRootFolder);
			if (!Directory.Exists(inventoryDir))
				return;

			var inventoryPathnames = new List<string>(Directory.GetFiles(inventoryDir, string.Format("{0}_??.{1}", SharedConstants.WordformInventory, SharedConstants.Inventory), SearchOption.TopDirectoryOnly));
			inventoryPathnames.Sort(StringComparer.InvariantCultureIgnoreCase);
			// the doc root will be "Inventory" (SharedConstants.WordformInventoryRootFolder).
			// This will store the PunctuationForm instances (unowned) in the header, and each PunctuationForm will be a child of header.
			// Each WfiWordform (unowned) will then be a child of root.
			foreach (var inventoryPathname in inventoryPathnames)
			{
				var doc = XDocument.Load(inventoryPathname);
				var unownedElements = new List<XElement>();
				var optionalHeaderElement = doc.Root.Element(SharedConstants.Header);
				if (optionalHeaderElement != null)
					unownedElements.AddRange(doc.Root.Element(SharedConstants.Header).Elements());
				var wordformElements = doc.Root.Elements("WfiWordform").ToList();
				if (wordformElements.Any())
					unownedElements.AddRange(wordformElements);
				// Query skips the dummy WfiWordform, if it is present.
				foreach (var unownedElement in unownedElements
					.Where(element => element.Attribute(SharedConstants.GuidStr).Value.ToLowerInvariant() != SharedConstants.EmptyGuid))
				{
					CmObjectFlatteningService.FlattenObject(
						inventoryPathname,
						sortedData,
						unownedElement,
						null); // Not owned.
				}
			}
		}
	}
}