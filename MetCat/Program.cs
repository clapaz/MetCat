using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetCat {
	public static class StringExtensions {
		public static bool Contains(this string source, string compare, StringComparison key) {
			return source.IndexOf(compare, key) >= 0;
		}
	}

	class Program {
		static bool IsFileLocked(System.IO.FileInfo file) {
			System.IO.FileStream stream = null;
			try {
				stream = file.Open(System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None);
			} catch(System.IO.IOException) {
				//the file is unavailable 
				return true;
			} finally {
				if(stream != null)
					stream.Close();
			}
			//file is not locked
			return false;
		}

		static void Main(string[] args) {
			//Import command line arguments
			string[] _args = Environment.GetCommandLineArgs();
			//string[] _args = new[] { "-i","L-37457 batch 1.pdf","-o","L-37457 batch 1.pdf","-bic","1","-ro","-v","-t","1" };

			//Parse arguments and set relevant flags
			//-i|--input : Comma delimited list, space optional, of 
			// // all input file names with .pdf assumed, directory
			// // seperately stored for * (all) inputs, if omitted
			// // then cat all files in local directory
			//-o|--output : Output file name, will overwrite, .pdf assumed
			//-b|--batch : Single number to denote the size of batch files
			//-v|--verbose : Output all logging lines
			//-r|--recursive : When given an all command will search
			// // In the directory with recursion
			//-h|--help : Prints the debug help lines
			string _input = "";
			string _directory = Environment.CurrentDirectory;
			string _output = "";
			int _batchSize = 0;
			int _totalAmtPages = 0;
			bool _verbose = false;
			int _verPN = 0;
			int _verPD = 0;
			bool _recursive = false;
			bool _retain = false;
			int _batchByImageComparison = 0;
			float _bicTolerance = 1.0f;

			for (int n = 0; n < _args.Length; n++) {
				if (_args[n].Equals("-i", StringComparison.OrdinalIgnoreCase) || _args[n].Equals("--input", StringComparison.OrdinalIgnoreCase)) {
					_input = _args[n + 1];
					_input = _input.Trim(new char[] { ' ', ',' });
				}
				if (_args[n].Equals("-o", StringComparison.OrdinalIgnoreCase) || _args[n].Equals("--output", StringComparison.OrdinalIgnoreCase)) {
					_output = _args[n + 1];
					_output = _output.Trim(new char[] { ' ', ',' });
					if (!_output.Contains(".pdf", StringComparison.OrdinalIgnoreCase)) {
						_output = _output + ".pdf";
					}
				}
				if(_args[n].Equals("-b", StringComparison.OrdinalIgnoreCase) || _args[n].Equals("--batch", StringComparison.OrdinalIgnoreCase)) {
					Int32.TryParse(_args[n + 1], out _batchSize);
				}
				if(_args[n].Equals("-bic", StringComparison.OrdinalIgnoreCase) || _args[n].Equals("--batchByImageComparison", StringComparison.OrdinalIgnoreCase)) {
					Int32.TryParse(_args[n + 1], out _batchByImageComparison);
				}
				if(_args[n].Equals("-t", StringComparison.OrdinalIgnoreCase) || _args[n].Equals("--tolerance", StringComparison.OrdinalIgnoreCase)) {
					_bicTolerance = float.Parse(_args[n + 1], System.Globalization.NumberStyles.Float);
				}
				if(_args[n].Equals("-ro", StringComparison.OrdinalIgnoreCase) || _args[n].Equals("--retainOriginal", StringComparison.OrdinalIgnoreCase)) {
					_retain = true;
				}
				if (_args[n].Equals("-v", StringComparison.OrdinalIgnoreCase) || _args[n].Equals("--verbose", StringComparison.OrdinalIgnoreCase)) {
					_verbose = true;
				}
				if (_args[n].Equals("-r", StringComparison.OrdinalIgnoreCase) || _args[n].Equals("--recursive", StringComparison.OrdinalIgnoreCase)) {
					_recursive = true;
				}
				if (_args[n].Equals("-h", StringComparison.OrdinalIgnoreCase) || _args[n].Equals("--help", StringComparison.OrdinalIgnoreCase)) {
					Console.WriteLine("Thank you for using MetCat\nA Windows 64-bit pdf concatenation tool\nImplementation of iTextSharp under the AGPL\nThe following are accepted arguments\n");
					Console.WriteLine("-i|--input\nComma delimited list (,) with no spaces of all input file names with .pdf assumed. File names containing commas will cause errors. Directory seperately stored for * (all) inputs. If omitted then cat all files in local directory\neg: -i 'C:/User/Files/MyPDFs/*.pdf'\n");
					Console.WriteLine("-o|--output\nOutput file name, will overwrite, .pdf assumed.\neg: -o 'C:/User/Desktop/SaveLocation/The Output.pdf'\n");
					Console.WriteLine("-b|--batch\n(Optional) A single number denoting the size of each output batch file in pages. Appends _Batch_# to the end of the output file name\neg: -b 500\n");
					Console.WriteLine("-bic|--batchByImageComparison\n(Optional) A single number denoting a page to use as a master page in image comparison. Will then batch the document by image comparison based on this master page.\neg: -bic 27\n");
					Console.WriteLine("-t|--tolerance\n(Optional) A single floating point number denoting the maximum tolerance of a -bic command in percent. Defaults to %1.0.\neg: -t 42.876\n");
					Console.WriteLine("-ro|--retainOriginal\n(Optional) Retain the original file when batching, don't delete it\n");
					Console.WriteLine("-v|--verbose\n(Optional) Output all logging lines\n");
					Console.WriteLine("-r|--recursive\n(Optional) When given an all command will search in the directory with recursion\n");
					Console.WriteLine("-h|--help\n(Optional) Prints these debug help lines");
					Environment.Exit(0);
				}
			}

			//Force quit if input or output are undefined
			if (_input == "") {
				Console.WriteLine("No input file was specified\n");
				_input = "/*.pdf";
			}
			if (_output == "") {
				Console.WriteLine("No output file was specified\n");
				_output = _directory + "/MetCatOutput.pdf";
			}

			//Handle * input to pull all matching files
			//If input contains
			List<string> _allInputs = new List<string>();
			if (_input.Contains("*")) {
				_input = _input.Remove(_input.IndexOf('*'));
				if(_input.Length < 5) {
					_input = _directory + _input;
				}
				Console.WriteLine(_input);
				System.IO.DirectoryInfo directorySearch = new System.IO.DirectoryInfo(_input);
				_input = "";
				System.IO.FileInfo[] allFiles = null;
				if (_verbose) {
					Console.WriteLine("Getting all files in " + directorySearch.Name + " with recursion = " + _recursive.ToString());
				}
				if (!_recursive) {
					allFiles = directorySearch.GetFiles("*.pdf", System.IO.SearchOption.TopDirectoryOnly);
				} else {
					allFiles = directorySearch.GetFiles("*.pdf", System.IO.SearchOption.AllDirectories);
				}
				if (_verbose) {
					Console.WriteLine("Creating file list\n0% Complete");
					_verPD = allFiles.Length;
					_verPN = 0;
				}
				foreach (System.IO.FileInfo file in allFiles) {
					_allInputs.Add(file.FullName);
					if (_verbose) {
						try {Console.SetCursorPosition(0, Console.CursorTop - 1);} catch{}
						_verPN++;
						float pcnt = ((float)_verPN / (float)_verPD);
						Console.WriteLine(pcnt.ToString("%0.0") + " Complete");
					}
				}
			}

			//Split the inputs/output into a string array by comma
			string[] _inputS = _input.Split(',');
			if (_allInputs.Count > 0) {
				_inputS = _allInputs.Select(i => i.ToString()).ToArray();
			}
			List<string> _validInputs = new List<string>();
			List<string> _invalidInputs = new List<string>();
			string[] _outputS = { _output };
			List<string> _validOutput = new List<string>();
			List<string> _invalidOutput = new List<string>();

			//Check file paths
			//Update invalid paths prepended with directory
			if (_verbose) {
				Console.WriteLine("Performing input file path validation");
			}
			checkFilePaths(_validInputs, _invalidInputs, _inputS, _directory, false, _verbose);
			if (_verbose) {
				Console.WriteLine("Performing output file path validation");
			}
			checkFilePaths(_validOutput, _invalidOutput, _outputS, _directory, true, _verbose);

			//Force quit if there are no valid inputs/outputs
			if (_validInputs.Count <= 0) {
				Console.WriteLine("No input files were valid\n");
				Environment.Exit(3);
			}
			if (_validOutput.Count <= 0) {
				Console.WriteLine("No output file was valid\n");
				Environment.Exit(4);
			}

			//Check if the system has sufficient free space to handle the operation
			//Warn on low space, error on insufficient
			long totalSize = 0;
			foreach (string item in _validInputs) {
				System.IO.FileInfo fileInfo = new System.IO.FileInfo(item);
				totalSize += fileInfo.Length;
			}
			try {
				System.IO.DriveInfo drvInfo = new System.IO.DriveInfo("C");
				if (_verbose) {
					Console.WriteLine("Estimated Output Size = " + totalSize.ToString() + " bytes");
					Console.WriteLine("Estimated Free Sapce  = " + drvInfo.AvailableFreeSpace.ToString() + " bytes\n");
				}
				if (totalSize >= drvInfo.AvailableFreeSpace * 0.9) {
					Console.WriteLine("The drive does not have enough available space\n");
					Environment.Exit(5);
				} else if (totalSize >= drvInfo.AvailableFreeSpace * 0.5) {
					Console.WriteLine("The drive may not have enough available space\nThis operation will continue but may fill the drive\nThis would cause errors\n");
				}
			} catch {
				Console.WriteLine("Could not determine if sufficient space exists.");
				Console.WriteLine("Estimated Output Size = " + totalSize.ToString() + " bytes");
				Console.WriteLine("Attempting to create file regardless.\n");
			}

			//Cat pdf onto the output one at a time
			if(_batchSize > 0) {
				pdfOutputBatches(_output, _validInputs, _verbose, _totalAmtPages, _batchSize, _retain);
			} else if (_batchByImageComparison > 0) {
				batchByImageComparison(_validInputs[0], _batchByImageComparison, _bicTolerance, _verbose, _retain);
			} else {
				pdfOutput(_output, _validInputs, _verbose);
			}
		}

		static void checkFilePaths(List<string> validlist, List<string> invalidlist, string[] array, string directory, bool storeNonExisting, bool verbose) {
			//Check if the file paths provided are valid, invalid, and exist
			//Push items to valid and invalid arrays
			//Print lines for non-existance
			int num = 0;
			int dem = 0;
			if (verbose) {
				dem = array.Length;
				Console.WriteLine("0% Complete");
			}
			foreach (string arritem in array) {
				System.IO.FileInfo fileTest = null;
				string item = arritem;
				try {
					if (!item.Contains(".pdf", StringComparison.OrdinalIgnoreCase)) {
						item = item + ".pdf";
					}
					fileTest = new System.IO.FileInfo(item);
				} catch (ArgumentException) { } catch (System.IO.PathTooLongException) { } catch (NotSupportedException) { }
				if (ReferenceEquals(fileTest, null)) {
					//Invalid file name
					invalidlist.Add(item);
					if (verbose) {
						dem++;
					}
				} else if (fileTest.Exists) {
					//Valid file name and file exists
					validlist.Add(item);
				} else {
					//Valid file name but file doesn't exist
					if (storeNonExisting) {
						validlist.Add(item);
					} else {
						Console.WriteLine(item + " : Doesn't exist\n");
					}
				}
				if (verbose) {
					try {Console.SetCursorPosition(0, Console.CursorTop - 1);} catch{}
					num++;
					float pcnt = ((float)num / (float)dem);
					Console.WriteLine(pcnt.ToString("%0.0") + " Complete");
				}
			}

			for (int n = 0; n < invalidlist.Count; n++) {
				invalidlist[n] = directory + invalidlist[n];
			}
			array = null;
			array = invalidlist.Select(i => i.ToString()).ToArray();

			foreach (string item in array) {
				System.IO.FileInfo fileTest = null;
				try {
					fileTest = new System.IO.FileInfo(item);
				} catch (ArgumentException) { } catch (System.IO.PathTooLongException) { } catch (NotSupportedException) { }
				if (ReferenceEquals(fileTest, null)) {
					//Invalid file name
					Console.WriteLine(item + " : Is an invalid file name\n");
				} else if (fileTest.Exists) {
					//Valid file name and file exists
					validlist.Add(item);
				} else {
					//Valid file name but file doesn't exist
					if (storeNonExisting) {
						validlist.Add(item);
					} else {
						Console.WriteLine(item + " : Doesn't exist\n");
					}
				}
				if (verbose) {
					try {Console.SetCursorPosition(0, Console.CursorTop - 1);} catch{}
					num++;
					float pcnt = ((float)num / (float)dem);
					Console.WriteLine(pcnt.ToString("%0.0") + " Complete");
				}
			}

		}

		//Outputs a new PDF to the _output from the _validInputs
		static string pdfOutput(string _output, List<string> _validInputs, bool _verbose) {
			if(_output.Contains(".pdf", StringComparison.OrdinalIgnoreCase)) {
				_output = _output.Remove(_output.IndexOf(".pdf", StringComparison.OrdinalIgnoreCase));
			}
			System.IO.Stream pdfOutStream = null;
			while(true) {
				int n = 0;
				try {
					if(n == 0) {
						pdfOutStream = new System.IO.FileStream(_output + ".pdf", System.IO.FileMode.Create);
						_output = _output + ".pdf";
					} else {
						pdfOutStream = new System.IO.FileStream(_output + " (" + n.ToString() + ").pdf", System.IO.FileMode.Create);
						_output = _output + " (" + n.ToString() + ").pdf";
					}
					break;
				} catch(System.IO.IOException) { } catch {
					Console.WriteLine("There was an error writing to the output file\nRenaming with iteration");
					n++;
				}
			}
			iTextSharp.text.pdf.PdfConcatenate Cat = null;
			iTextSharp.text.pdf.PdfReader pdfInStream = null;
			try {
				Cat = new iTextSharp.text.pdf.PdfConcatenate(pdfOutStream);
			} catch {
				Console.WriteLine("There was an error writing to the output file\nIs it in use?");
				Environment.Exit(7);
			}
			int sucCat = 0;
			foreach(string item in _validInputs) {
				try {
					pdfInStream = new iTextSharp.text.pdf.PdfReader(item);
					Cat.AddPages(pdfInStream);
					pdfInStream.Dispose();
					sucCat++;
					if(_verbose) {
						try { Console.SetCursorPosition(0, Console.CursorTop - 1); } catch { }
						Console.WriteLine(sucCat + " of " + _validInputs.Count + " Succeeded Concatenation");
					}
				} catch(iTextSharp.text.DocumentException) { } catch(System.IO.IOException) { } catch {
					Console.WriteLine(item + " Failed Concatenation\n");
				}
			}
			Cat.Close();
			pdfOutStream.Dispose();
			return _output;
		}

		//Outputs a new PDF to the _output from the _validInput
		//Output is constrained to the input page ranges for batching
		static void pdfOutputBatch(string _output, string _input, int _batchSize, bool _verbose) {
			//Clean the output string
			if(_output.Contains(".pdf", StringComparison.OrdinalIgnoreCase)) {
				_output = _output.Remove(_output.IndexOf(".pdf", StringComparison.OrdinalIgnoreCase));
			}
			//Define reused vars
			int currentPage = 0;
			int numBatches = 0;
			int numPages = 0;
			iTextSharp.text.pdf.PdfConcatenate Cat = null;
			System.IO.Stream pdfOutStream = null;

			//Open the input file
			iTextSharp.text.pdf.PdfReader pdfInStream = new iTextSharp.text.pdf.PdfReader(_input);
			numPages = pdfInStream.NumberOfPages;

			//Determine the amount of batches
			numBatches = (int)(numPages / _batchSize) + 1;

			//Loop for the amount of batches
			for(int n = 1; n <= numBatches; n++) {
				try {
					//Open the output file
					pdfOutStream = new System.IO.FileStream(_output + "_Batch_" + n.ToString() + ".pdf", System.IO.FileMode.Create);
					Cat = new iTextSharp.text.pdf.PdfConcatenate(pdfOutStream);

					//Get the page range, accounting for file end
					int start = currentPage+1;
					currentPage = start + _batchSize - 1;
					if(currentPage > numPages) currentPage = numPages;
					if (_verbose) Console.WriteLine("From: " + start.ToString() + "\tTo: " + currentPage.ToString());
					//pdfInStream.SelectPages(iTextSharp.text.pdf.SequenceList.Expand(start.ToString() + "-" + currentPage.ToString(), numPages), false);

					//Cat the page range onto the output
					Cat.AddPages(pdfInStream, start, currentPage);

					//Close the cat
					Cat.Close();
					pdfOutStream.Dispose();
				} catch(Exception e) {
					Console.WriteLine("There was an error in the Batch Process");
					Console.WriteLine(e);
				}
			}
			pdfInStream.Dispose();
		}

		static void pdfOutputBatches(string _output, List<string> _validInputs, bool _verbose, int _totalAmtPages, int _batchSize, bool retain) {
			if(_validInputs.Count > 1) {
				Console.WriteLine("\nCannot batch multiple files\nPerforming Cat and batch on result\n\n");
				Random rInt = new Random();
				string rInts = rInt.Next().ToString() + rInt.Next().ToString() + rInt.Next().ToString() + rInt.Next().ToString() + rInt.Next().ToString();
				string newout = pdfOutput(_output + "TempForBatch" + rInts + ".pdf", _validInputs, _verbose);
				_validInputs.Clear();
				_validInputs.Add(newout);
			}

			pdfOutputBatch(_output, _validInputs[0], _batchSize, _verbose);

			if(!retain) {
				System.IO.FileInfo outFile = new System.IO.FileInfo(_validInputs[0]);
				while(IsFileLocked(outFile)) System.Threading.Thread.Sleep(500);
				outFile.Delete();
			}
		}

		static string pdfRenderAsImage(string InputPDFFile, int PageNumber, int resolution) {
			string outImageName = System.IO.Path.GetFileNameWithoutExtension(InputPDFFile);
			outImageName = Environment.CurrentDirectory + "/" + outImageName + "_Pg_" + PageNumber.ToString() + ".png";
			
			
			Ghostscript.NET.GhostscriptPngDevice dev = new Ghostscript.NET.GhostscriptPngDevice(Ghostscript.NET.GhostscriptPngDeviceType.Png256);
			dev.GraphicsAlphaBits = Ghostscript.NET.GhostscriptImageDeviceAlphaBits.V_4;
			dev.TextAlphaBits = Ghostscript.NET.GhostscriptImageDeviceAlphaBits.V_4;
			dev.ResolutionXY = new Ghostscript.NET.GhostscriptImageDeviceResolution(resolution, resolution);
			dev.InputFiles.Add(InputPDFFile);
			dev.Pdf.FirstPage = PageNumber;
			dev.Pdf.LastPage = PageNumber;
			dev.CustomSwitches.Add("-dDOINTERPOLATE");
			dev.OutputPath = outImageName;
			dev.Process();

			return outImageName;
		}

		static bool batchByImageComparison(string _input, int masterPage, float tolerance, bool verbose, bool retain) {
			int Resolution = 16;
			System.IO.FileInfo outFile;
			bool match = false;
			int curPage = 0;
			int bookmark = 0;
			iTextSharp.text.pdf.PdfReader pdfInStream = new iTextSharp.text.pdf.PdfReader(_input);
			int numPages = pdfInStream.NumberOfPages;
			string img1 = pdfRenderAsImage(_input, masterPage, Resolution);
			//System.Drawing.Image image1 = System.Drawing.Image.FromFile(img1);
			System.Drawing.Bitmap bmp1 = new System.Drawing.Bitmap(img1);
			string img2 = "";
			//System.Drawing.Image image2;

			//Loop over all pages
			while(curPage <= numPages) {
				curPage++;
				if(curPage != masterPage) {
					img2 = pdfRenderAsImage(_input, curPage, Resolution);
					//image2 = System.Drawing.Image.FromFile(img2);
				} else {
					img2 = img1;
					//image2 = (System.Drawing.Image)image1.Clone();
				}

				//Compare the two images as linear pixel 4D Distance
				System.Drawing.Bitmap bmp2 = new System.Drawing.Bitmap(img2);
				System.Drawing.Color Color1, Color2;
				//float A, R, G, B;
				//double SSD;
				float totalDifference = 0.0f;
				for(int x = 0; x < bmp1.Width; x++) {
					for(int y = 0; y < bmp1.Height; y++) {
						try {
							Color1 = bmp1.GetPixel(x, y);
							Color2 = bmp2.GetPixel(x, y);
							//A = Color1.A - Color2.A;
							//R = Color1.R - Color2.R;
							//G = Color1.G - Color2.G;
							//B = Color1.B - Color2.B;
							//A *= A; R *= R; G *= G; B *= B;
							//SSD = A + R + G + B;
							//totalDifference += Math.Sqrt(SSD);
							totalDifference += Math.Abs((float)(Color1.A - Color2.A)) / 255.0f;
							totalDifference += Math.Abs((float)(Color1.R - Color2.R)) / 255.0f;
							totalDifference += Math.Abs((float)(Color1.G - Color2.G)) / 255.0f;
							totalDifference += Math.Abs((float)(Color1.B - Color2.B)) / 255.0f;
						} catch(Exception e) {
							Console.WriteLine("Exception occurred at (" + x.ToString() + "," + y.ToString() + ")\n" + e);
							Environment.Exit(0);
						}
					}
				}
				bmp2.Dispose();

				//Remove the second image
				if(curPage != masterPage) {
					//image2.Dispose();
					outFile = new System.IO.FileInfo(img2);
					while(IsFileLocked(outFile)) System.Threading.Thread.Sleep(500);
					outFile.Delete();
				}

				//Mark the page if this is considered a match
				//If there is already a mark then create a batch from the mark to the current area and reset the mark
				totalDifference = (100 * totalDifference) / (bmp1.Width * bmp1.Height * 4);
				if(verbose) Console.WriteLine("--Difference of : %" + totalDifference.ToString() + "\tOn : " + curPage);
					if(totalDifference < tolerance) {
					if(bookmark == 0) {
						bookmark = curPage;
					} else {
						System.IO.Stream pdfOutStream = new System.IO.FileStream(_input + "_Pages_From_" + bookmark.ToString() + "_to_" + (curPage-1).ToString() + ".pdf", System.IO.FileMode.Create);
						iTextSharp.text.pdf.PdfConcatenate Cat = new iTextSharp.text.pdf.PdfConcatenate(pdfOutStream);
						Cat.AddPages(pdfInStream, bookmark, curPage-1);
						Cat.Close();
						pdfOutStream.Dispose();
						bookmark = curPage;
						match = true;
					}
				}
			}

			//Remove the first image
			//image1.Dispose();
			bmp1.Dispose();
			outFile = new System.IO.FileInfo(img1);
			while(IsFileLocked(outFile)) System.Threading.Thread.Sleep(500);
			outFile.Delete();

			if(!retain) {
				System.IO.FileInfo inputFile = new System.IO.FileInfo(_input);
				while(IsFileLocked(inputFile)) System.Threading.Thread.Sleep(500);
				inputFile.Delete();
			}

			return match;
		}
	}
}