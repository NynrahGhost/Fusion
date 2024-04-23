using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using System.Security.Cryptography;
using UAssetAPI.FieldTypes;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Diagnostics;
using UAssetAPI.UnrealTypes;

class Program
{
    static void Main(string[] args)
    {
        string pathExeNoShipping;
        string pathExe;
        string pathMod;
        string pathPak;
        string pathCfg;

        string pathBaseCfg;
        string pathBasePak; //LogicMods folder if R2Modman exists
        string fileMainPak;  //VotV-WindowsNoEditor

        bool isManualOnly;
        bool isDebug = false;

        if (args.Length == 0)
        {   //Manual only
            pathMod = Environment.CurrentDirectory.Substring(0, Environment.CurrentDirectory.Length - "/NynrahGhost-Fusion".Length);
            pathExeNoShipping = pathMod.Substring(0, pathMod.Length - "/Mods".Length);

            string tmp = pathExeNoShipping.Substring(0, pathExeNoShipping.Length - "/Binaries/Win64".Length);

            pathPak = tmp + @"\Content\Paks\LogicMods";
            pathCfg = tmp + @"\Config";

            pathBaseCfg = "";
            pathBasePak = "";
            fileMainPak = pathPak.Substring(0, pathPak.Length - "/LogicMods".Length) + @"\VotV-WindowsNoEditor.pak";

            pathExe = tmp.Substring(0, tmp.Length - "/VotV".Length);

            isManualOnly = true;
        }
        else
        {   //Manual + R2Modman
            pathExeNoShipping = args[0];
            pathExe = pathExeNoShipping.Substring(0, pathExeNoShipping.Length - @"\VotV\Binaries\Win64".Length);
            pathMod = args[1];
            pathPak = args[2];
            pathCfg = args[3];

            pathBaseCfg = pathExe + @"\Config";
            pathBasePak = pathExe + @"\Content\Paks\LogicMods";
            fileMainPak = pathExe + @"\VotV\Content\Paks\VotV-WindowsNoEditor.pak";

            isManualOnly = false;
        }

        string pathExtracted = pathMod + @"\NynrahGhost-Fusion\Extracted\";
        string fileFusionCfg = pathCfg + @"\Fusion-config.ini";
        string fileLastWrite = pathMod + @"\NynrahGhost-Fusion\last.bin";
        string filePass = pathMod + @"\NynrahGhost-Fusion\pass";
        string fileThunderstoreModsYml = pathMod.Substring(0, pathMod.Length - @"\shimloader\mod".Length) + @"\mods.yml";
        string fileHash = pathMod + @"\NynrahGhost-Fusion\hash.bin";
        string filePatchPak = pathPak + @"\NynrahGhost-Fusion\VotV-WindowsNoEditor_p.pak";
        string fileExtractedDefaultInput = pathMod + @"\NynrahGhost-Fusion\Extracted\VotV-WindowsNoEditor_p\VotV\Config\DefaultInput.ini";
        string pathAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string fileInput = pathAppData + @"\VotV\Saved\Config\WindowsNoEditor\Input.ini";



        if (File.Exists(filePass))
        {
            File.Delete(filePass);
            return;
        }

        if (File.Exists(fileFusionCfg))
        {
            string res = File.ReadAllText(fileFusionCfg);
            if (res.StartsWith("debug=") && res.Substring("debug=".Length).StartsWith("true"))
                isDebug = true;
        }

        /*
        if (File.Exists(fileLastWrite) && !isDebug)
        {
            var lastModUpdate = File.GetLastWriteTime(fileThunderstoreModsYml);
            var lastFusion = DateTime.Parse(File.ReadAllText(fileLastWrite));
            if (lastFusion > lastModUpdate)
                return;
        }*/

        List<string> modFoldersToIgnore = new List<string>();

        if (!isManualOnly)
        {
            string[] list = File.ReadAllLines(fileThunderstoreModsYml);

            for (int index = 0; index < list.Length; ++index)
            {
                string tmp;
                if (list[index].StartsWith("  name: "))
                {
                    tmp = list[index].Substring("  name: ".Length);
                    for (++index; index < list.Length; ++index)
                    {
                        if (list[index].StartsWith("  name: "))
                            break;
                        if (list[index].StartsWith("  enabled: "))
                        {
                            if (list[index]["  enabled: ".Length] == 'f')
                                modFoldersToIgnore.Add(tmp);
                            break;
                        }
                    }
                }
            }
        }

        SHA256 hashing = SHA256.Create();


        FileInfo[] PakFiles = new FileInfo[0];
        FileInfo[] PakFilesManual = new FileInfo[0];
        FileInfo[] CfgFiles = new FileInfo[0];
        FileInfo[] CfgFilesManual = new FileInfo[0];

        if (Directory.Exists(pathMod))
            PakFiles = new DirectoryInfo(pathPak).GetFiles("*.pak", SearchOption.AllDirectories);

        for (int i = 0; i < PakFiles.Length; ++i)
        {
            if (PakFiles[i].Name == @"VotV-WindowsNoEditor_p.pak")
            {
                for (int j = i + 1; j < PakFiles.Length; ++j)
                    PakFiles[j - 1] = PakFiles[j];
                Array.Resize(ref PakFiles, PakFiles.Length - 1);
                break;
            }
        }

        if (Directory.Exists(pathBasePak))
            PakFilesManual = new DirectoryInfo(pathBasePak).GetFiles("*.pak", SearchOption.AllDirectories);

        Array.Resize(ref PakFiles, PakFiles.Length + PakFilesManual.Length);
        Array.Copy(PakFilesManual, 0, PakFiles, PakFiles.Length - PakFilesManual.Length, PakFilesManual.Length);

        if (Directory.Exists(pathCfg))
            CfgFiles = new DirectoryInfo(pathCfg).GetFiles("*DefaultInput.ini", SearchOption.AllDirectories);

        if (Directory.Exists(pathBaseCfg))
            CfgFilesManual = new DirectoryInfo(pathBaseCfg).GetFiles("*DefaultInput.ini", SearchOption.AllDirectories);

        Array.Resize(ref PakFiles, PakFiles.Length + PakFilesManual.Length);
        Array.Copy(PakFilesManual, 0, PakFiles, PakFiles.Length - PakFilesManual.Length, PakFilesManual.Length);

        Array.Resize(ref CfgFiles, CfgFiles.Length + CfgFilesManual.Length);
        Array.Copy(CfgFilesManual, 0, CfgFiles, CfgFiles.Length - CfgFilesManual.Length, CfgFilesManual.Length);

        //Checking timestamps on whether anything changed
        {
            string[] dateStamps;
            if (File.Exists(fileLastWrite))
                dateStamps = File.ReadAllLines(fileLastWrite);
            else goto filesRewritten;

            int i = -1;
            if(dateStamps.Length != (PakFiles.Length + CfgFiles.Length))
                goto filesRewritten;
            for (int p = 0; p < PakFiles.Length; ++p)
            {
                if (DateTime.Parse(dateStamps[++i]).Equals(File.GetLastWriteTime(PakFiles[p].FullName)))
                    goto filesRewritten;
            }
            for (int c = 0; c < CfgFiles.Length; ++c)
            {
                if (DateTime.Parse(dateStamps[++i]).Equals(File.GetLastWriteTime(CfgFiles[c].FullName)))
                    goto filesRewritten;
            }
            return;
        }

        filesRewritten:

        var tmpHash = new List<byte>(32 * (PakFiles.Length + CfgFiles.Length));

        foreach (FileInfo pak in PakFiles)
        {
            try
            {
                using (FileStream stream = pak.Open(FileMode.Open))
                    tmpHash.AddRange(hashing.ComputeHash(stream));
            } catch (IOException e)
            {
                foreach (var p in FileUtil.WhoIsLocking(pak.FullName))
                {
                    p.Kill();
                    p.WaitForExit();
                }
                using (FileStream stream = pak.Open(FileMode.Open))
                    tmpHash.AddRange(hashing.ComputeHash(stream));
            }
        }
        foreach (FileInfo cfg in CfgFiles)
        {
            using (FileStream stream = cfg.Open(FileMode.Open))
                tmpHash.AddRange(hashing.ComputeHash(stream));
        }
        byte[] resHash = hashing.ComputeHash(tmpHash.ToArray());

        if (File.Exists(fileHash) && resHash.SequenceEqual(File.ReadAllBytes(fileHash)) && !isDebug)
        {
            goto StartVotV; //Stop Fusion if everything is already nice
        }


        if (File.Exists(filePatchPak))  //Case R2Modman
            File.Delete(filePatchPak);

        if (Directory.Exists(pathExtracted))    //Re-creating since some mods can be deleted and uassets will be left over,
            Directory.Delete(pathExtracted, true);    //making it into the resulting VotV-WindowsNoEditor_p
        Directory.CreateDirectory(pathExtracted); 

        //MainPak
        ProcessStartInfo UPakProgramInfo = new ProcessStartInfo(
            pathMod + @"\NynrahGhost-Fusion\repak",
            "unpack " +
            "-f " +
            "-i=\"VotV/Content/main/datatables\" " + //Later add -i=../main/enums
            //"-i=\"VotV/Content/main/enums\" " + //Later add -i=../main/interfaces
            "-i=\"VotV/Config\" " +
            "-o=\"Extracted\\VotV-WindowsNoEditor_p\" \"" +
            fileMainPak + "\"");

        UPakProgramInfo.WorkingDirectory = pathMod + @"\NynrahGhost-Fusion\";
        UPakProgramInfo.UseShellExecute = false;
        UPakProgramInfo.CreateNoWindow = true;
        Process.Start(UPakProgramInfo).WaitForExit();


        //ModPaks
        foreach (FileInfo file in PakFiles)
        {
            if(modFoldersToIgnore.Contains(file.Directory.Name)) 
                continue;
            UPakProgramInfo.Arguments = "unpack -f -i=\"VotV/Content/Mods/" + Path.GetFileNameWithoutExtension(file.Name) + "/_Content\" -o=\"Extracted/" + Path.GetFileNameWithoutExtension(file.Name) + "\" \"" + file.FullName + "\""; // /main/datatables
            Process.Start(UPakProgramInfo).WaitForExit();

            string modExtractedPath = pathExtracted +
                Path.GetFileNameWithoutExtension(file.Name) +
                @"\VotV\Content\Mods\" +
                Path.GetFileNameWithoutExtension(file.Name) +
                @"\_Content";

            if (Directory.Exists(modExtractedPath))
            {
                var dirsToTransfer = Directory.GetDirectories(modExtractedPath);
                foreach (var dir in dirsToTransfer)
                {
                    var dirInfo = new DirectoryInfo(dir);
                    if (dirInfo.Name == "main")
                        continue;
                    if (Directory.Exists(pathMod + @"\NynrahGhost-Fusion\Extracted\VotV-WindowsNoEditor_p\VotV\Content\" + dirInfo.Name))
                        Directory.Delete(pathMod + @"\NynrahGhost-Fusion\Extracted\VotV-WindowsNoEditor_p\VotV\Content\" + dirInfo.Name, true);
                    Directory.Move(dir, pathMod + @"\NynrahGhost-Fusion\Extracted\VotV-WindowsNoEditor_p\VotV\Content\" + dirInfo.Name);
                }
            }
        }


        //Fusing datatables
        {
            FileInfo[] UMainAssetFiles = new DirectoryInfo(pathMod + @"\NynrahGhost-Fusion\Extracted\VotV-WindowsNoEditor_p\VotV\Content\main\datatables").GetFiles("*.uasset");

            foreach (FileInfo file in PakFiles)
            {
                FileInfo[] UAssetFiles;
                {
                    var path = pathExtracted + Path.GetFileNameWithoutExtension(file.Name) + @"\VotV\Content\Mods\" + Path.GetFileNameWithoutExtension(file.Name) + @"\_Content\main\datatables";
                    if (Directory.Exists(path))
                        UAssetFiles = new DirectoryInfo(path).GetFiles("*.uasset");
                    else
                        continue;
                }

                foreach (FileInfo uassetFile in UAssetFiles)
                {
                    foreach (FileInfo mainUAssetFile in UMainAssetFiles)
                    {
                        if (Path.GetFileNameWithoutExtension(mainUAssetFile.Name) == Path.GetFileNameWithoutExtension(uassetFile.Name.Substring(1)))
                        {
                            UAsset mainUAssetHandle = new UAsset(mainUAssetFile.FullName, UAssetAPI.UnrealTypes.EngineVersion.VER_UE4_27);
                            DataTableExport dataTableMain = (DataTableExport)mainUAssetHandle.Exports[0];

                            UAsset uassetHandle = new UAsset(uassetFile.FullName, UAssetAPI.UnrealTypes.EngineVersion.VER_UE4_27);
                            DataTableExport dataTable = (DataTableExport)uassetHandle.Exports[0];

                            mainUAssetHandle.PackageGuid = uassetHandle.PackageGuid;

                            //Adding entries to Name Map
                            foreach (var name in uassetHandle.GetNameMapIndexList())
                                mainUAssetHandle.AddNameReference(name);


                            //Adding entries to Import Data
                            int importOffset = mainUAssetHandle.Imports.Count();
                            foreach (var import in uassetHandle.Imports)
                            {
                                import.ClassPackage.Index = mainUAssetHandle.AddNameReference(import.ClassPackage.Value);
                                import.ClassName.Index = mainUAssetHandle.AddNameReference(import.ClassName.Value);
                                import.ObjectName.Index = mainUAssetHandle.AddNameReference(import.ObjectName.Value);

                                if (import.OuterIndex.Index != 0)
                                    import.OuterIndex.Index -= importOffset;  //Unsafe

                                mainUAssetHandle.AddImport(import);
                            }


                            //mainUAssetHandle.Imports.AddRange(uassetHandle.Imports);

                            var EntryMain = dataTableMain.Table.Data[0];
                            foreach (var entry in dataTable.Table.Data)
                            {
                                entry.Name.Index = mainUAssetHandle.AddNameReference(entry.Name.Value);

                                var listMain = (List<PropertyData>)EntryMain.RawValue;
                                var list = (List<PropertyData>)entry.RawValue;

                                for (int i = 0; i < listMain.Count; ++i)
                                {
                                    if (list[i] is ObjectPropertyData)
                                    {
                                        ((ObjectPropertyData)list[i]).Value.Index -= importOffset;  //Unsafe
                                    }
                                    else if (list[i] is NamePropertyData)
                                    {
                                        ((NamePropertyData)list[i]).Value.Index = mainUAssetHandle.AddNameReference(((NamePropertyData)list[i]).Value.Value);
                                    }
                                    else if (list[i] is BytePropertyData)
                                    {
                                        ((BytePropertyData)list[i]).EnumType.Index = mainUAssetHandle.AddNameReference(((BytePropertyData)list[i]).EnumType.Value);
                                        ((BytePropertyData)list[i]).EnumValue.Index = mainUAssetHandle.AddNameReference(((BytePropertyData)list[i]).EnumValue.Value);
                                    }
                                    list[i].Name = listMain[i].Name;
                                }

                                dataTableMain.Table.Data.Add(entry);
                            }

                            Console.WriteLine(dataTableMain.Table.Data.Count);

                            mainUAssetHandle.Exports[0] = dataTableMain;

                            mainUAssetHandle.Write(mainUAssetFile.FullName);

                            break;
                        }
                    }
                }
            }
        }

        //Fusing enums
        /*
        {
            FileInfo[] UMainAssetFiles = new DirectoryInfo(pathMod + @"\NynrahGhost-Fusion\Extracted\VotV-WindowsNoEditor_p\VotV\Content\main\enums").GetFiles("*.uasset");

            foreach (FileInfo file in PakFiles)
            {
                FileInfo[] UAssetFiles;
                {
                    var path = pathExtracted + Path.GetFileNameWithoutExtension(file.Name) + @"\VotV\Content\Mods\" + Path.GetFileNameWithoutExtension(file.Name) + @"\_Content\main\enums";
                    if (Directory.Exists(path))
                        UAssetFiles = new DirectoryInfo(path).GetFiles("*.uasset");
                    else
                        continue;
                }

                foreach (FileInfo uassetFile in UAssetFiles)
                {
                    foreach (FileInfo mainUAssetFile in UMainAssetFiles)
                    {
                        if (Path.GetFileNameWithoutExtension(mainUAssetFile.Name) == Path.GetFileNameWithoutExtension(uassetFile.Name.Substring(1)))
                        {
                            UAsset mainUAssetHandle = new UAsset(mainUAssetFile.FullName, UAssetAPI.UnrealTypes.EngineVersion.VER_UE4_27);
                            EnumExport enumMain = (EnumExport)mainUAssetHandle.Exports[0];

                            UAsset uassetHandle = new UAsset(uassetFile.FullName, UAssetAPI.UnrealTypes.EngineVersion.VER_UE4_27);
                            EnumExport enumMod = (EnumExport)uassetHandle.Exports[0];

                            mainUAssetHandle.PackageGuid = uassetHandle.PackageGuid;


                            //Adding entries to Name Map
                            foreach (var name in uassetHandle.GetNameMapIndexList())
                                mainUAssetHandle.AddNameReference(name);


                            //Adding entries to Import Data | Maybe not needed?
                            int importOffset = mainUAssetHandle.Imports.Count();
                            foreach (var import in uassetHandle.Imports)
                            {
                                import.ClassPackage.Index = mainUAssetHandle.AddNameReference(import.ClassPackage.Value);
                                import.ClassName.Index = mainUAssetHandle.AddNameReference(import.ClassName.Value);
                                import.ObjectName.Index = mainUAssetHandle.AddNameReference(import.ObjectName.Value);

                                if (import.OuterIndex.Index != 0)
                                    import.OuterIndex.Index -= importOffset;  //Unsafe

                                mainUAssetHandle.AddImport(import);
                            }

                            enumMain.Enum.Names.AddRange(enumMod.Enum.Names);

                            List<PropertyData> keys = new List<PropertyData>(((MapPropertyData)enumMain.Data[0]).Value.Keys);
                            List<PropertyData> values = new List<PropertyData>(((MapPropertyData)enumMain.Data[0]).Value.Values);
                            List<PropertyData> modValues = new List<PropertyData>(((MapPropertyData)enumMod.Data[0]).Value.Values);

                            foreach(var val in modValues)
                            {
                                Fname keyName = new FName(mainUAssetHandle.NameMap);

                            }
                            ((MapPropertyData)enumMain.Data[0]).Value.Add()

                            //ENDED HERE


                            //mainUAssetHandle.Imports.AddRange(uassetHandle.Imports);

                            //var list = ((MapPropertyData)enumMain.Data[0]).Value.Cont;
                            //list.Value.Add(new NamePropertyData(), new TextPropertyData());

                            //enumMod

                            //enumMain.Enum.Names[0].Item1.
                            /*
                            var EntryMain = dataTableMain.Table.Data[0];
                            foreach (var entry in dataTable.Table.Data)
                            {
                                entry.Name.Index = mainUAssetHandle.AddNameReference(entry.Name.Value);

                                var listMain = (List<PropertyData>)EntryMain.RawValue;
                                var list = (List<PropertyData>)entry.RawValue;

                                for (int i = 0; i < listMain.Count; ++i)
                                {
                                    if (list[i] is ObjectPropertyData)
                                    {
                                        ((ObjectPropertyData)list[i]).Value.Index -= importOffset;  //Unsafe
                                    }
                                    else if (list[i] is NamePropertyData)
                                    {
                                        ((NamePropertyData)list[i]).Value.Index = mainUAssetHandle.AddNameReference(((NamePropertyData)list[i]).Value.Value);
                                    }
                                    else if (list[i] is BytePropertyData)
                                    {
                                        ((BytePropertyData)list[i]).EnumType.Index = mainUAssetHandle.AddNameReference(((BytePropertyData)list[i]).EnumType.Value);
                                        ((BytePropertyData)list[i]).EnumValue.Index = mainUAssetHandle.AddNameReference(((BytePropertyData)list[i]).EnumValue.Value);
                                    }
                                    list[i].Name = listMain[i].Name;
                                }

                                dataTableMain.Table.Data.Add(entry);
                            }

                            Console.WriteLine(dataTableMain.Table.Data.Count);

                            mainUAssetHandle.Exports[0] = dataTableMain;

                            mainUAssetHandle.Write(mainUAssetFile.FullName);
                            

                            break;
                        }
                    }
                }
            }
        }

        */

        //Fusing DefaultInput
        var defaultInputCfg = File.AppendText(fileExtractedDefaultInput);
        List<string> inputs = new List<string>();

        foreach (FileInfo file in new DirectoryInfo(pathMod + @"\NynrahGhost-Fusion\Extracted\").GetFiles("DefaultInput.ini"))
        {
            if (file.Directory.Name == "VotV-WindowsNoEditor")
                continue;
            inputs.AddRange(File.ReadAllLines(file.FullName));
            defaultInputCfg.Write("\n#" + file.Directory.Name + "\\" + file.Name + "\n");
            defaultInputCfg.Write(File.ReadAllText(file.FullName));
            defaultInputCfg.Write("\n");
        }

        foreach (FileInfo file in CfgFiles)
        {
            inputs.AddRange(File.ReadAllLines(file.FullName));
            defaultInputCfg.Write("\n#" + file.Directory.Name + "\\" + file.Name + "\n");
            defaultInputCfg.Write(File.ReadAllText(file.FullName));
            defaultInputCfg.Write("\n");
        }

        defaultInputCfg.Close();

        {
            string userInput = File.ReadAllText(fileInput);
            var fileHandle = File.AppendText(fileInput);

            foreach(var inputString in inputs)
            {
                var iStart = inputString.IndexOf("\"");
                var iEnd = inputString.IndexOf("\"", iStart+1);
                if (iEnd == -1)
                    continue;
                if (!userInput.Contains(inputString.Substring(iStart, iEnd-iStart+1)))
                {
                    if (inputString.StartsWith('+') | inputString.StartsWith('-'))
                        fileHandle.WriteLine(inputString.Substring(1));
                    else
                        fileHandle.WriteLine(inputString);
                }
            }

            fileHandle.Close();
        }

        /*
        var defaultInputCfg = new List<string>(File.ReadAllLines(fileExtractedDefaultInput));
        {
            int AxisConfig = 0, ActionMappings = 0, AxisMappings = 0;
            for(int index = 0; index < defaultInputCfg.Count; ++index)
            {
                if(defaultInputCfg[index].Length > "+AxisConfig".Length)
                    switch (defaultInputCfg[index].Substring(0, "+AxisConfig".Length))
                    {
                        case "+AxisConfig": AxisConfig = index; break;
                        case "+ActionMapp": ActionMappings = index; break;
                        case "+AxisMappin": AxisMappings = index; break;
                    }
            }
            
            foreach (FileInfo file in new DirectoryInfo(pathMod + @"\NynrahGhost-Fusion\Extracted\").GetFiles("DefaultInput.ini"))
            {
                if (file.Directory.Name == "VotV-WindowsNoEditor")
                    continue;

                string[] fileLines = File.ReadAllLines(file.FullName);

                for (int index = 0; index < fileLines.Length; ++index)
                {
                    switch (fileLines[index].Substring(0, "+AxisConfig".Length))
                    {
                        case "+AxisConfig": defaultInputCfg.Insert(++AxisConfig, fileLines[index]); break;
                        case "+ActionMapp": defaultInputCfg.Insert(++ActionMappings, fileLines[index]); break;
                        case "+AxisMappin": defaultInputCfg.Insert(++AxisMappings, fileLines[index]); break;
                    }
                }
            }

            foreach (FileInfo file in CfgFiles)
            {
                string[] fileLines = File.ReadAllLines(file.FullName);

                for (int index = 0; index < fileLines.Length; ++index)
                {
                    switch (fileLines[index].Substring(0, "+AxisConfig".Length))
                    {
                        case "+AxisConfig": defaultInputCfg.Insert(++AxisConfig, fileLines[index]); break;
                        case "+ActionMapp": defaultInputCfg.Insert(++ActionMappings, fileLines[index]); break;
                        case "+AxisMappin": defaultInputCfg.Insert(++AxisMappings, fileLines[index]); break;
                    }
                }
            }

            File.Delete(fileExtractedDefaultInput);
            File.WriteAllLines(fileExtractedDefaultInput, defaultInputCfg);
        }
        */

        //Pak-ing results
        UPakProgramInfo.Arguments = @"pack -m=../../../VotV/ --version=V11 ""Extracted/VotV-WindowsNoEditor_p/VotV/"" ""Extracted/VotV-WindowsNoEditor_p.pak""";
        System.Diagnostics.Process.Start(UPakProgramInfo).WaitForExit();

        Directory.CreateDirectory(pathPak + @"\NynrahGhost-Fusion");
        File.Move(pathExtracted + @"VotV-WindowsNoEditor_p.pak", filePatchPak);


        File.WriteAllBytes(fileHash, resHash);

        //File.WriteAllText(fileLastWrite, DateTime.Now.ToString());
        //Write date stamps of changes
        {
            var lines = new List<string>();
            foreach (FileInfo pak in PakFiles)
                lines.Add(File.GetLastWriteTime(pak.FullName).ToString());
            foreach (FileInfo cfg in CfgFiles)
                lines.Add(File.GetLastWriteTime(cfg.FullName).ToString());
            File.WriteAllLines(fileLastWrite, lines);
        }
        

    StartVotV:

        if (isManualOnly)
            System.Diagnostics.Process.Start(pathExeNoShipping + @"\VotV-Win64-Shipping.exe");//, args);
        else {
            File.Create(filePass);
            System.Diagnostics.Process.Start(pathExe + @"\VotV.exe", "--mod-dir \""+pathMod+"\" --pak-dir \""+pathPak+"\" --cfg-dir \""+pathCfg+"\"");
        }
        //System.Diagnostics.Process.Start(pathExe + @"\VotV.exe");

    }
}