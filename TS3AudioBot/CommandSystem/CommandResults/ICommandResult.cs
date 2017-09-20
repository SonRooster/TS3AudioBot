// TS3AudioBot - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2017  TS3AudioBot contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

namespace TS3AudioBot.CommandSystem
{
	using Helper;

	public abstract class ICommandResult
	{
		public abstract CommandResultType ResultType { get; }

		public override string ToString()
		{
			switch (ResultType)
			{
			case CommandResultType.String:
				return ((StringCommandResult)this).Content;
			case CommandResultType.Empty:
				return string.Empty;
			case CommandResultType.Command:
			case CommandResultType.Json:
				return "CommandResult can't be converted into a string";
			default:
				throw Util.UnhandledDefault(ResultType);
			}
		}
	}
}
