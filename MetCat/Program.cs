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
		static void Main(string[] args) {
			//Import command line arguments
			string[] _args = Environment.GetCommandLineArgs();

			//Parse arguments and set relevant flags
			//-i|--input : Comma delimited list, space optional, of 
			// // all input file names with .pdf assumed, directory
			// // seperately stored for * (all) inputs, if omitted
			// // then cat all files in local directory
			//-o|--output : Output file name, will overwrite, .pdf assumed
			//-v|--verbose : Output all logging lines
			//-r|--recursive : When given an all command will search
			// // In the directory with recursion
			//-h|--help : Prints the debug help lines
			string _input = "";
			string _directory = Environment.CurrentDirectory;
			string _output = "";
			bool _verbose = false;
			int _verPN = 0;
			int _verPD = 0;
			bool _recursive = false;

			for (int n = 0; n < _args.Length; n++) {
				if (_args[n].Contains("-i", StringComparison.OrdinalIgnoreCase) || _args[n].Contains("--input", StringComparison.OrdinalIgnoreCase)) {
					_input = _args[n + 1];
					_output = _output.Trim(new char[] { ' ', ',' });
				}
				if (_args[n].Contains("-o", StringComparison.OrdinalIgnoreCase) || _args[n].Contains("--output", StringComparison.OrdinalIgnoreCase)) {
					_output = _args[n + 1];
					_output = _output.Trim(new char[] { ' ', ',' });
					if (!_output.Contains(".pdf", StringComparison.OrdinalIgnoreCase)) {
						_output = _output + ".pdf";
					}
				}
				if (_args[n].Contains("-v", StringComparison.OrdinalIgnoreCase) || _args[n].Contains("--verbose", StringComparison.OrdinalIgnoreCase)) {
					_verbose = true;
				}
				if (_args[n].Contains("-r", StringComparison.OrdinalIgnoreCase) || _args[n].Contains("--recursive", StringComparison.OrdinalIgnoreCase)) {
					_recursive = true;
				}
				if (_args[n].Contains("-h", StringComparison.OrdinalIgnoreCase) || _args[n].Contains("--help", StringComparison.OrdinalIgnoreCase)) {
					Console.WriteLine("Thank you for using MetCat\nA Windows 64-bit pdf concatenation tool\nImplementation of iTextSharp under the AGPL\nThe following are accepted arguments\n\n-i|--input\nComma delimited list (,) with no spaces of all input file names with .pdf assumed. File names containing commas will cause errors. Directory seperately stored for * (all) inputs. If omitted then cat all files in local directory\neg: -i 'C:/User/Files/MyPDFs/*.pdf'\n\n-o|--output\nOutput file name, will overwrite, .pdf assumed.\neg: -o 'C:/User/Desktop/SaveLocation/The Output.pdf'\n\n-v|--verbose\n(Optional)Output all logging lines\n\n-r|--recursive\n(Optional)When given an all command will search in the directory with recursion\n\n-h|--help\n(Optional)Prints these debug help lines");
					Environment.Exit(0);
				}
			}

			//Force quit if input or output are undefined
			if (_input == "") {
				Console.WriteLine("No input file was specified\n");
				_input = _directory + "/*.pdf";
			}
			if (_output == "") {
				Console.WriteLine("No output file was specified\n");
				Environment.Exit(2);
			}

			//Handle * input to pull all matching files
			//If input contains
			List<string> _allInputs = new List<string>();
			if (_input.Contains("*")) {
				_input = _input.Remove(_input.IndexOf('*'));
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
						Console.SetCursorPosition(0, Console.CursorTop - 1);
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
			System.IO.Stream pdfOutStream = null;
			try {
				pdfOutStream = new System.IO.FileStream(_output, System.IO.FileMode.Create);
			} catch (System.IO.IOException) { } catch {
				Console.WriteLine("There was an error writing to the output file\nIs it in use?");
				Environment.Exit(7);
			}
			iTextSharp.text.pdf.PdfConcatenate Cat = new iTextSharp.text.pdf.PdfConcatenate(pdfOutStream);
			iTextSharp.text.pdf.PdfReader pdfInStream = null;
			int sucCat = 0;
			foreach (string item in _validInputs) {
				try {
					pdfInStream = new iTextSharp.text.pdf.PdfReader(item);
					Cat.AddPages(pdfInStream);
					pdfInStream.Dispose();
					sucCat++;
					if (_verbose) {
						Console.SetCursorPosition(0, Console.CursorTop - 1);
						Console.WriteLine(sucCat + " of " + _validInputs.Count + " Succeeded Concatenation");
					}
				} catch (iTextSharp.text.DocumentException) { } catch (System.IO.IOException) { } catch {
					Console.WriteLine(item + " Failed Concatenation\n");
					pdfInStream.Dispose();
				}
			}
			Cat.Close();
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
					Console.SetCursorPosition(0, Console.CursorTop - 1);
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
					Console.SetCursorPosition(0, Console.CursorTop - 1);
					num++;
					float pcnt = ((float)num / (float)dem);
					Console.WriteLine(pcnt.ToString("%0.0") + " Complete");
				}
			}

		}
	}
}