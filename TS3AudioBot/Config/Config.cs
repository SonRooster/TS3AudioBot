// TS3AudioBot - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2017  TS3AudioBot contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

namespace TS3AudioBot.Config
{
	using Helper;
	using Localization;
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text.RegularExpressions;

	public partial class ConfRoot
	{
		private static readonly Regex BotFileMatcher = new Regex(@"^bot_(.+)\.toml$", Util.DefaultRegexConfig);

		private string fileName;
		private readonly Dictionary<string, ConfBot> botConfCache = new Dictionary<string, ConfBot>();

		public static R<ConfRoot> Open(string file)
		{
			var loadResult = Load<ConfRoot>(file);
			if (!loadResult.Ok)
			{
				Log.Error(loadResult.Error, "Could not load core config.");
				return R.Err;
			}

			if (!loadResult.Value.CheckAndSet(file))
				return R.Err;
			return loadResult.Value;
		}

		public static R<ConfRoot> Create(string file)
		{
			var newFile = CreateRoot<ConfRoot>();
			if (!newFile.CheckAndSet(file))
				return newFile;
			var saveResult = newFile.Save(file, true);
			if (!saveResult.Ok)
			{
				Log.Error(saveResult.Error, "Failed to save config file '{0}'.", file);
				return R.Err;
			}
			return newFile;
		}

		public static R<ConfRoot> OpenOrCreate(string file) => File.Exists(file) ? Open(file) : Create(file);

		private bool CheckAndSet(string file)
		{
			fileName = file;
			if (!CheckPaths())
				return false;
			// further checks...
			return true;
		}

		private bool CheckPaths()
		{
			try
			{
				if (!Directory.Exists(Configs.BotsPath.Value))
					Directory.CreateDirectory(Configs.BotsPath.Value);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Could not create bot config subdirectory.");
				return false;
			}
			return true;
		}

		public bool Save() => Save(fileName, false);

		// apply root_path to input path
		public string GetFilePath(string file)
		{
			throw new NotImplementedException();
		}

		internal R<string, LocalStr> NameToPath(string name)
		{
			var nameResult = Util.IsSafeFileName(name);
			if (!nameResult.Ok)
				return nameResult.Error;
			return Path.Combine(Configs.BotsPath.Value, $"bot_{name}.toml");
		}

		public ConfBot CreateBot()
		{
			var config = CreateRoot<ConfBot>();
			InitializeBotConfig(config);
			return config;
		}

		public ConfBot[] GetAllBots()
		{
			try
			{
				return Directory.EnumerateFiles(Configs.BotsPath.Value, "bot_*.toml", SearchOption.TopDirectoryOnly)
					.SelectOk(filePath => ExtractNameFromFile(new FileInfo(filePath).Name))
					.SelectOk(GetBotConfig)
					.ToArray();
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Could not access bot config subdirectory.");
				return null;
			}
		}

		private static R<string, LocalStr> ExtractNameFromFile(string file)
		{
			var match = BotFileMatcher.Match(file);
			if (match.Success && Util.IsSafeFileName(match.Groups[1].Value))
				return match.Groups[1].Value;
			return Util.IsSafeFileName(file).WithValue(file);
		}

		public R<ConfBot, Exception> GetBotConfig(string name)
		{
			var file = NameToPath(name);
			if (!file.Ok)
				return new Exception(file.Error.Str);
			if (!botConfCache.TryGetValue(name, out var botConf))
			{
				var botConfResult = Load<ConfBot>(file.Value);
				if (!botConfResult.Ok)
				{
					Log.Warn(botConfResult.Error, "Failed to load bot config \"{0}\"", name);
					return botConfResult.Error;
				}
				botConf = botConfResult.Value;
				InitializeBotConfig(botConf);
				botConf.Name = name;
				botConfCache[name] = botConf;
			}
			return botConf;
		}

		private void InitializeBotConfig(ConfBot config)
		{
			Bot.Derive(config);
			config.Parent = this;
		}

		public void ClearBotConfigCache()
		{
			botConfCache.Clear();
		}

		public void ClearBotConfigCache(string name)
		{
			botConfCache.Remove(name);
		}

		internal void AddToConfigCache(ConfBot config)
		{
			var name = config.Name;
			if (!string.IsNullOrEmpty(name) && !botConfCache.ContainsKey(name))
				botConfCache[name] = config;
		}

		public E<LocalStr> CreateBotConfig(string name)
		{
			var file = NameToPath(name);
			if (!file.Ok)
				return file.Error;
			if (File.Exists(file.Value))
				return new LocalStr("The file already exists."); // LOC: TODO
			try
			{
				using (File.Open(file.Value, FileMode.CreateNew))
					return R.Ok;
			}
			catch (Exception ex)
			{
				Log.Debug(ex, "Config file could not be created");
				return new LocalStr("Could not create config."); // LOC: TODO
			}
		}

		public E<LocalStr> DeleteBotConfig(string name)
		{
			var file = NameToPath(name);
			if (!file.Ok)
				return file.Error;
			if (botConfCache.TryGetValue(name, out var conf))
				conf.Name = null;
			botConfCache.Remove(name);
			if (!File.Exists(file.Value))
				return R.Ok;
			try
			{
				File.Delete(file.Value);
				return R.Ok;
			}
			catch (Exception ex)
			{
				Log.Debug(ex, "Config file could not be deleted");
				return new LocalStr("Could not delete config."); // LOC: TODO
			}
		}

		public E<LocalStr> CopyBotConfig(string from, string to)
		{
			var fileFrom = NameToPath(from);
			if (!fileFrom.Ok)
				return fileFrom.Error;
			var fileTo = NameToPath(to);
			if (!fileTo.Ok)
				return fileTo.Error;

			if (!File.Exists(fileFrom.Value))
				return new LocalStr("The source bot does not exist.");
			if (File.Exists(fileTo.Value))
				return new LocalStr("The target bot already exists, delete it before to overwrite.");

			try
			{
				File.Copy(fileFrom.Value, fileTo.Value, false);
				return R.Ok;
			}
			catch (Exception ex)
			{
				Log.Debug(ex, "Config file could not be copied");
				return new LocalStr("Could not copy config."); // LOC: TODO
			}
		}
	}

	public partial class ConfBot
	{
		public string Name { get; set; }

		public E<LocalStr> SaveNew(string name)
		{
			var parent = GetParent();
			var file = parent.NameToPath(name);
			if (!file.Ok)
				return file.Error;
			if (File.Exists(file.Value))
				return new LocalStr("The file already exists."); // LOC: TODO
			var result = SaveInternal(file.Value);
			if (!result.Ok)
				return result;
			Name = name;
			parent.AddToConfigCache(this);
			return R.Ok;
		}

		public E<LocalStr> SaveWhenExists()
		{
			if (string.IsNullOrEmpty(Name))
				return R.Ok;

			var file = GetParent().NameToPath(Name);
			if (!file.Ok)
				return file.Error;
			if (!File.Exists(file.Value))
				return R.Ok;
			return SaveInternal(file.Value);
		}

		private E<LocalStr> SaveInternal(string file)
		{
			var result = Save(file, false);
			if (!result.Ok)
			{
				Log.Error(result.Error, "An error occoured saving the bot config.");
				return new LocalStr(string.Format("An error occoured saving the bot config.")); // LOC: TODO
			}
			return R.Ok;
		}

		public ConfRoot GetParent() => Parent as ConfRoot;
	}
}
