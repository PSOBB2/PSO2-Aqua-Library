using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AquaModelLibrary;
using AquaModelLibrary.Native.Fbx;

namespace PSOBB2.ModelTool.FBXExporter
{
	class Program
	{
		public static List<string> modelExtensions = new List<string>() { ".aqp", ".aqo", ".trp", ".tro" };
		public static List<string> simpleModelExtensions = new List<string>() { ".prm", ".prx" };
		public static List<string> effectExtensions = new List<string>() { ".aqe" };
		public static List<string> motionConfigExtensions = new List<string>() { ".bti" };
		public static List<string> motionExtensions = new List<string>() { ".aqm", ".aqv", ".aqc", ".aqw", ".trm", ".trv", ".trw" };

		static async Task Main(string[] args)
		{
			// Basically commandline version of: private void batchPSO2ToFBXToolStripMenuItem_Click(object sender, EventArgs e)
			if (!args.Any())
				throw new InvalidOperationException($"You must provide the folder path to convert to args.");

			Console.WriteLine($"Glader PSOBB2 Automated PSO2 FBX Exporter based on Shadowth117's Aqua tools.");

			var folderPath = args.First();

			List<string> errorList = new();

			AquaUtil aqua = new AquaUtil();
			foreach(string filename in Directory.EnumerateFiles(folderPath, "*.aqp"))
			{
				try
				{
					AquaObject model;
					bool isPrm = false;
					var ext = Path.GetExtension(filename);
					if(simpleModelExtensions.Contains(ext))
					{
						aqua.LoadPRM(filename);
						aqua.ConvertPRMToAquaObject();
						isPrm = true;
					}
					else
					{
						if(modelExtensions.Contains(ext))
						{
							//Get bone ext
							string boneExt = "";
							switch(ext)
							{
								case ".aqo":
								case ".aqp":
									boneExt = ".aqn";
									break;
								case ".tro":
								case ".trp":
									boneExt = ".trn";
									break;
								default:
									break;
							}
							var bonePath = filename.Replace(ext, boneExt);
							aqua.aquaBones.Clear();
							if(!File.Exists(bonePath)) //We need bones for this
							{
								//Check group 1 if group 2 doesn't have them
								bonePath = bonePath.Replace("group2", "group1");
								if(!File.Exists(bonePath))
								{
									bonePath = null;
								}
							}
							if(bonePath != null)
							{
								aqua.ReadBones(bonePath);
							}
							else
							{
								//If we really can't find anything, make a placeholder
								aqua.aquaBones.Add(AquaNode.GenerateBasicAQN());
							}
						}
						aqua.ReadModel(filename);
					}


					// exportLODModelsIfInSameaqpToolStripMenuItem default is FALSE
					//var modelCount = !isPrm && exportLODModelsIfInSameaqpToolStripMenuItem.Checked ? aquaUI.aqua.aquaModels[0].models.Count : 1;
					int modelCount = 1;

					for(int i = 0; i < aqua.aquaModels[0].models.Count && i < modelCount; i++)
					{
						model = aqua.aquaModels[0].models[i];
						if(!isPrm && model.objc.type > 0xC32)
						{
							model.splitVSETPerMesh();
						}
						model.FixHollowMatNaming();

						var name = Path.ChangeExtension(filename, ".fbx");
						if(modelCount > 1)
						{
							name = Path.Combine(Path.GetDirectoryName(name), Path.GetFileNameWithoutExtension(name) + $"_{i}.fbx");
						}

						Console.WriteLine($"Exporting: {Path.GetFileNameWithoutExtension(filename)}");
						// includeMetadataToolStripMenuItem default is TRUE
						// FbxExporter.ExportToFile(model, aqua.aquaBones[0], new List<AquaMotion>(), name, new List<string>(), new List<Matrix4x4>(), includeMetadataToolStripMenuItem.Checked);
						FbxExporter.ExportToFile(model, aqua.aquaBones[0], new List<AquaMotion>(), name, new List<string>(), new List<Matrix4x4>(), true);
					}
					aqua.aquaBones.Clear();
					aqua.aquaModels.Clear();
				}
				catch (Exception e)
				{
					Console.WriteLine($"Error: {e}");
					errorList.Add($"Model: {Path.GetFileNameWithoutExtension(filename)} failed to export. Reason: {e.ToString()}");
				}

				await File.WriteAllLinesAsync("errors.txt", errorList);
			}
		}
	}
}
