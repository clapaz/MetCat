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

			//Get the total number of pages in the cat
			iTextSharp.text.pdf.PdfReader pdfItem = null;
			foreach(string item in _validInputs) {
				try {
					pdfItem = new iTextSharp.text.pdf.PdfReader(item);
					_totalAmtPages += pdfItem.NumberOfPages;
					pdfItem.Dispose();
					if(_verbose) {
						Console.WriteLine("Getting total amount of pages: " + _totalAmtPages.ToString());
					}
				} catch(iTextSharp.text.DocumentException) { } catch(System.IO.IOException) { } catch {
					Console.WriteLine(item + " Failed Concatenation\n");
					_validInputs.Remove(item);
					pdfItem.Dispose();
				}
			}

			//Cat pdf onto the output one at a time
			if(_batchSize > 0) {
				pdfOutputBatches(_output, _validInputs, _verbose, _totalAmtPages, _batchSize, _retain);
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
		static void pdfOutput(string _output, List<string> _validInputs, bool _verbose) {
			System.IO.Stream pdfOutStream = null;
			try {
				pdfOutStream = new System.IO.FileStream(_output, System.IO.FileMode.Create);
			} catch(System.IO.IOException) { } catch {
				Console.WriteLine("There was an error writing to the output file\nIs it in use?");
				Environment.Exit(7);
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
					pdfInStream.Dispose();
				}
			}
			Cat.Close();
		}

		//Outputs a new PDF to the _output from the _validInput
		//Output is constrained to the input page ranges for batching
		static void pdfOutputBatch(string _output, List<string> _validInputs, bool _verbose, int _remainingPages, int _preceedingPages) {
			System.IO.Stream pdfOutStream = null;
			try {
				pdfOutStream = new System.IO.FileStream(_output, System.IO.FileMode.Create);
			} catch(System.IO.IOException) { } catch {
				Console.WriteLine("There was an error writing to the output file\nIs it in use?");
				Environment.Exit(7);
			}
			iTextSharp.text.pdf.PdfConcatenate Cat = null;
			iTextSharp.text.pdf.PdfReader pdfInStream = null;
			try {
				Cat = new iTextSharp.text.pdf.PdfConcatenate(pdfOutStream);
			} catch {
				Console.WriteLine("There was an error writing to the output file\nIs it in use?");
				Environment.Exit(7);
			}
			try {
				pdfInStream = new iTextSharp.text.pdf.PdfReader(_validInputs[0]);
				int stopPage = _preceedingPages + _remainingPages - 1;
				if(stopPage > pdfInStream.NumberOfPages) {
					pdfInStream.SelectPages(_preceedingPages.ToString() + "-" + pdfInStream.NumberOfPages.ToString());
				} else {
					pdfInStream.SelectPages(_preceedingPages.ToString() + "-" + stopPage.ToString());
				}
				Cat.AddPages(pdfInStream);
				pdfInStream.Dispose();
				if(_verbose) {
					Console.WriteLine((stopPage - _preceedingPages + 1).ToString() + " Pages Batched");
				}
			} catch(iTextSharp.text.DocumentException) { } catch(System.IO.IOException) { } catch {
				Console.WriteLine(_validInputs[0] + " Failed Batching Process\n");
				pdfInStream.Dispose();
			}
			Cat.Close();
		}

		static void pdfOutputBatches(string _output, List<string> _validInputs, bool _verbose, int _totalAmtPages, int _batchSize, bool retain) {
			if(_validInputs.Count > 1) {
				Console.WriteLine("\nCannot batch multiple files\nPerforming Cat and batch on result\n\n");
				pdfOutput(_output + "TempForBatch.pdf", _validInputs, _verbose);
				_validInputs.Clear();
				_validInputs.Add(_output + "TempForBatch.pdf");
			}
			iTextSharp.text.pdf.PdfReader pdfItem = new iTextSharp.text.pdf.PdfReader(_validInputs[0]);
			if(pdfItem.NumberOfPages < _batchSize) {
				pdfOutput(_output, _validInputs, _verbose);
				if(!retain) { System.IO.File.Delete(_validInputs[0]); }
				return;
			}
			if(_output.Contains(".pdf", StringComparison.OrdinalIgnoreCase)) {
				_output = _output.Remove(_output.IndexOf(".pdf", StringComparison.OrdinalIgnoreCase));
			}
			int _numBatches = (int)(_totalAmtPages / _batchSize) + 1;
			int _currentBatch = 1;
			int _previousPages = 1;
			//Loop over each individual output batch
			for(int n = 0; n < _numBatches; n++) {
				if(_verbose) {
					Console.WriteLine("Beginning batch #" + _currentBatch.ToString());
				}
				pdfOutputBatch(_output + "_Batch_" + _currentBatch.ToString() + ".pdf", _validInputs, _verbose, _batchSize, _previousPages);
				_previousPages += _batchSize;
				_currentBatch++;
			}
			pdfItem.Dispose();
			if(!retain) { System.IO.File.Delete(_validInputs[0]); }
		}
	}
}