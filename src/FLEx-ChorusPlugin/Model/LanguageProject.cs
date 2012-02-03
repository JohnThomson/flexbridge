﻿using System;
using System.IO;
using FLEx_ChorusPlugin.Properties;

namespace FLEx_ChorusPlugin.Model
{
	/// <summary>
	/// LanguageProject represents a FieldWorks language project
	/// (but is not the same class as is used in FielwdWorks).
	///
	/// A LanguageProject may, or may not, be enabled for remote collaboration use.
	/// If it is not ready for such use, it can be made ready.
	///
	/// The expectation is that the main data file to be used will have an extension of 'fwdata'
	/// and will be an xml file.
	/// </summary>
	internal sealed class LanguageProject
	{
		public static string OBTAINPROJECT = "Obtain Project";
		private readonly string _fwdataFile;

		internal LanguageProject(string fwdataFile)
		{
			if (string.IsNullOrEmpty(fwdataFile))
			{
				throw new ArgumentNullException("fwdataFile");
			}
			if (!File.Exists(fwdataFile) && fwdataFile != OBTAINPROJECT)
			{
				throw new FileNotFoundException("Cannot find the file.", fwdataFile);
			}
			if ((!Path.HasExtension(fwdataFile) || Path.GetExtension(fwdataFile) != ".fwdata")
				&& fwdataFile != OBTAINPROJECT)
			{
				throw new ArgumentException(Resources.kNotAnFwXmlFile, "fwdataFile");
			}

			_fwdataFile = fwdataFile;
		}

		internal string Name
		{
			get { return Path.GetFileNameWithoutExtension(_fwdataFile); }
		}

		internal string DirectoryName
		{
			get { return Path.GetDirectoryName(_fwdataFile); }
		}

		internal bool IsRemoteCollaborationEnabled
		{
			get
			{
				return Directory.Exists(Path.Combine(DirectoryName, ".hg"));
			}
		}

		internal bool FieldWorkProjectInUse
		{
			get
			{
				var lockPathname = Path.Combine(DirectoryName, Name + ".fwdata.lock");
				return File.Exists(lockPathname);
			}
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
