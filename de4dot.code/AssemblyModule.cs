﻿/*
    Copyright (C) 2011-2012 de4dot@gmail.com

    This file is part of de4dot.

    de4dot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    de4dot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with de4dot.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.IO;
using System.Collections.Generic;
using dot10.DotNet;
using dot10.DotNet.Writer;
using de4dot.blocks;

namespace de4dot.code {
	class AssemblyModule {
		string filename;
		ModuleDefMD module;
		ModuleContext moduleContext;

		public AssemblyModule(string filename, ModuleContext moduleContext) {
			this.filename = Utils.getFullPath(filename);
			this.moduleContext = moduleContext;
		}

		public ModuleDefMD load() {
			return setModule(ModuleDefMD.Load(filename, moduleContext));
		}

		public ModuleDefMD load(byte[] fileData) {
			return setModule(ModuleDefMD.Load(fileData, moduleContext));
		}

		ModuleDefMD setModule(ModuleDefMD newModule) {
			module = newModule;
			TheAssemblyResolver.Instance.addModule(module);
			module.EnableTypeDefFindCache = true;
			module.Location = filename;
			return module;
		}

		public void save(string newFilename, bool preserveTokens, bool updateMaxStack, IModuleWriterListener writerListener) {
			MetaDataFlags mdFlags = 0;
			if (!updateMaxStack)
				mdFlags |= MetaDataFlags.KeepOldMaxStack;
			if (preserveTokens)
				mdFlags |= MetaDataFlags.PreserveTokens | MetaDataFlags.PreserveUSOffsets | MetaDataFlags.PreserveExtraSignatureData;

			if (module.IsILOnly) {
				var writerOptions = new ModuleWriterOptions(module, writerListener);
				writerOptions.MetaDataOptions.Flags |= mdFlags;
				writerOptions.Logger = Logger.Instance;
				module.Write(newFilename, writerOptions);
			}
			else {
				var writerOptions = new NativeModuleWriterOptions(module, writerListener);
				writerOptions.MetaDataOptions.Flags |= mdFlags;
				writerOptions.Logger = Logger.Instance;
				module.NativeWrite(newFilename, writerOptions);
			}
		}

		public ModuleDefMD reload(byte[] newModuleData, DumpedMethodsRestorer dumpedMethodsRestorer, IStringDecrypter stringDecrypter) {
			TheAssemblyResolver.Instance.removeModule(module);
			var mod = ModuleDefMD.Load(newModuleData, moduleContext);
			if (dumpedMethodsRestorer != null)
				dumpedMethodsRestorer.Module = mod;
			mod.StringDecrypter = stringDecrypter;
			mod.MethodDecrypter = dumpedMethodsRestorer;
			mod.TablesStream.ColumnReader = dumpedMethodsRestorer;
			mod.TablesStream.MethodRowReader = dumpedMethodsRestorer;
			return setModule(mod);
		}

		public override string ToString() {
			return filename;
		}
	}
}
