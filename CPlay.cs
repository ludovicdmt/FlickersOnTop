﻿using System;
using System.Linq;
using System.Threading.Tasks;
using SDL2;
using System.Runtime.InteropServices;
using System.Collections;
using System.Windows.Forms;
using System.Threading;
using LSL;
using System.Xml;
using System.Globalization;
using System.Drawing;
using System.Collections.Generic;

namespace VisualStimuli
{
    
    public class CPlay
	{
        private long TICKSPERSECONDS = TimeSpan.TicksPerSecond;
        ArrayList m_listFlickers = new ArrayList();
		string filepath; //indicate the environment path
		public CPlay(string filepath)
		{
			this.filepath = filepath;
		}
		StreamOutlet outl;

        public CPlay()
		{
			//lookup the position of the Application
            filepath = Application.StartupPath;
            // create stream info and outlet
            StreamInfo inf = new StreamInfo("flickers_info", "Markers", 1, 0, channel_format_t.cf_string, "giu4h5600");
            outl = new StreamOutlet(inf);
        }

		[DllImport("user32.dll", EntryPoint = "SetWindowPos")]
		public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
		public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
		public const UInt32 SWP_NOSIZE = 0x0001;
		public const UInt32 SWP_NOMOVE = 0x0002;
		public const UInt32 SWP_SHOWWINDOW = 0x0040;

		[DllImport("user32.dll")]
		public static extern int EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);
		[DllImport("user32.dll")]
		public static extern int EnumDisplayDevices(string deviceName,int index,out DEVMODE devMode);


        /// https://csharp.hotexamples.com/site/file?hash=0x238a44f2ed8da632e4e090af60159d66126fa38d9fd574bee99939a492be5ee7&fullName=KernelAPI.cs&project=ozeppi/mAgicAnime
        [StructLayout(LayoutKind.Sequential)]
		public struct DEVMODE
		{
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
			public string dmDeviceName;
			public System.Int16 dmSpecVersion;
			public System.Int16 dmDriverVersion;
			public System.Int16 dmSize;
			public System.Int16 dmDriverExtra;
			public System.Int32 dmFields;
			public System.Int16 dmOrientation;
			public System.Int16 dmPaperSize;
			public System.Int16 dmPaperLength;
			public System.Int16 dmPaperWidth;
			public System.Int16 dmScale;
			public System.Int16 dmCopies;
			public System.Int16 dmDefaultSource;
			public System.Int16 dmPrintQuality;
			public System.Int16 dmColor;
			public System.Int16 dmDuplex;
			public System.Int16 dmYResolution;
			public System.Int16 dmTTOption;
			public System.Int16 dmCollate;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
			public string dmFormName;
			public System.Int16 dmUnusedPadding;
			public System.Int16 dmBitsPerPel;
			public System.Int32 dmPelsWidth;
			public System.Int32 dmPelsHeight;
			public System.Int32 dmDisplayFlags;
			public System.Int32 dmDisplayFrequency;
		}
        public enum Signal_Type //enum for reading the xml file
        {
            Sine = 1,
            Root_Square = 3,
            Square = 2,
            Random = 0,
            Maximum_Lenght_Sequence = 4,
			None=5
        };

        /// <summary>
        /// Read a Xml file and add the flickers to the list of active flickers.
        /// </summary>
        /// <param name="filePath">Path to the xml file.</param>
        public void Read_File(string filePath)
		{
			try {
				// Load the XML file into memory
				XmlDocument doc = new XmlDocument();
				doc.Load(filePath);

				// Get the root element of the document
				XmlElement root = doc.DocumentElement;

                //program instance for window creation
                var instance=Marshal.GetHINSTANCE(this.GetType().Module);

                int k = 0;
				// Iterate over the child elements of the root
				foreach (XmlNode node in root.ChildNodes)
				{
                    int.TryParse(node.SelectSingleNode("X").InnerText, out int pos_x);
                    int.TryParse(node.SelectSingleNode("Y").InnerText, out int pos_y);
					int.TryParse(node.SelectSingleNode("Width").InnerText, out int width);
					int.TryParse(node.SelectSingleNode("Height").InnerText, out int height);
                    Enum.TryParse<Signal_Type>(node.SelectSingleNode("Type").InnerText, out Signal_Type type);
                    string name = node.SelectSingleNode("Name").InnerText;
                    double.TryParse(node.SelectSingleNode("Frequency").InnerText, NumberStyles.Number, CultureInfo.GetCultureInfo("en-US"), out double freq); //culture info is necessary due to use of "," or "." for decimal number in different part of the world
                    double.TryParse(node.SelectSingleNode("Phase").InnerText, NumberStyles.Number, CultureInfo.GetCultureInfo("en-US"), out double phase);
					var C1Node = node.SelectSingleNode("color1");
					var C2Node = node.SelectSingleNode("color2");
                    Byte.TryParse(C1Node.SelectSingleNode("R").InnerText, out byte r1);
                    Byte.TryParse(C1Node.SelectSingleNode("G").InnerText, out byte g1);
					Byte.TryParse(C1Node.SelectSingleNode("B").InnerText, out byte b1);
                    int.TryParse(node.SelectSingleNode("Opacity_Min").InnerText, out int a1);
                    int.TryParse(node.SelectSingleNode("Opacity_Max").InnerText, out int a2);
					string image = string.Empty;
                    bool.TryParse(node.SelectSingleNode("IsImageFlicker").InnerText, out bool IsImage);
                    if (IsImage)
					{
						image= node.SelectSingleNode("image").InnerText;
                    }
					int[] seq = new int[0];
                    if (node.SelectSingleNode("sequence") != null)
					{
                        var seqnodes = node.SelectSingleNode("sequence").ChildNodes;
                        seq = new int[seqnodes.Count];
                        for (int i = 0; i < seq.Length; i++)
                        {
                            int v;
                            int.TryParse(seqnodes[i].InnerText, out v);
                            seq.SetValue(v, i);
                        }
                    }
                    //create a window and add the flickers to the list of flickers
                    CScreen screen = new CScreen(pos_x, pos_y, width, height, name+k.ToString(), false,r1,g1,b1,image,
                       instance);
					m_listFlickers.Add(new CFlicker(
						name,
						pos_x,
						pos_y,
						width,
						height,
						screen,
					   Color.FromArgb(255, r1, g1, b1), // color1 RGB
					   freq,
					   (int)Math.Round(a1 * 2.55), // alpha1
					   (int)Math.Round(a2 * 2.55), // alpha2
					   phase,
					   (int)type,
					   seq)
					);
					k++;
                }
				Console.WriteLine("Created {0} Flickers", m_listFlickers.Count);
				
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				Console.WriteLine(e.StackTrace);
			}
        }

		/// <summary>
		/// Initiate Flickers
		/// </summary>
		/// <returns>None</returns>
		public void init_test()
		{
			Read_File(filepath+"\\Flickers.xml");
		}
		/// <summary>
		/// Get refresh rate of the screen
		/// </summary>
		/// <returns>Refresh rate of the screen</returns>
		public static double getFrameRate()
		{
            /*DEVMODE devMode = new DEVMODE();*/
			
			List<DEVMODE> l = getAllScreen();
			double frequencyMonitor = 0;
			for(int i=0;i<l.Count;i++)
			{
				DEVMODE devMode = l[i];
                devMode.dmSize = (short)Marshal.SizeOf(devMode);
                devMode.dmDriverExtra = 0;
                EnumDisplaySettings(null, -1, ref devMode);
				frequencyMonitor = frequencyMonitor > devMode.dmDisplayFrequency ? frequencyMonitor : devMode.dmDisplayFrequency;
            }
            return frequencyMonitor;
        }
		public static List<DEVMODE> getAllScreen()
		{
			int i = 0,a=1;
			List<DEVMODE> l = new List<DEVMODE>();
			while(a!=0){
				DEVMODE devMode = new DEVMODE();
				a=EnumDisplayDevices(null, i, out devMode);
				l.Add(devMode);
                i++;
			}
			return l ;
		}
        private static double frameRate = getFrameRate();
        private bool quit = false;
        /// <summary>
        /// Animate the flickers from the .xml file
        /// </summary>
        /// <returns>None</returns>
        public void Animate_Flicker()
		{
			init_test();
			string[] marker_info = new string[4];

			// All the flickers foreground 
			for (int j = 0; j < m_listFlickers.Count; j++) {
				CFlicker aFlicker = (CFlicker)m_listFlickers[j];
				SetWindowPos(aFlicker.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
			}

			// init vars


			for (int j = 0; j < m_listFlickers.Count; j++)
			{
				// Send into LSL the ID,frequency,phase and amplitude of the flickers
				CFlicker currentFlicker = (CFlicker)m_listFlickers[j];
				marker_info[0] = j.ToString();
				marker_info[1] = currentFlicker.Frequency.ToString(); 
				marker_info[2] = currentFlicker.Phase.ToString();
				marker_info[3] = ((Math.Abs(currentFlicker.Alpha2-currentFlicker.Alpha1)).ToString());

                // Push the marker in LSL that the flicker will flicker now
                outl.push_sample(marker_info);
            }
            
            long frame = 0;
            var watch = System.Diagnostics.Stopwatch.StartNew(); //watch for fps syncing
			int lost_frame = 0; //only used if computer is too slow to display all frames, we will jump over some frames
            long frame_ticks =(long) ((1d / frameRate) * TICKSPERSECONDS);
            SDL.SDL_Event evt = new SDL.SDL_Event();
            while (!quit && SDL.SDL_GetTicks() < 1000000000)
			{
				frame += 1;
                var watchFPSMax=System.Diagnostics.Stopwatch.StartNew();
                //parallel treatment for each flickers (useful when lots of flicker on slow computer with multiple proc core)
                //flickers are still synchronized, only parallelized during 1 frame.
                Parallel.ForEach<CFlicker>(m_listFlickers.Cast<CFlicker>(), c =>
				{
                    if (c.index >= c.size) { c.index = 0; }
                    //check if this flicker is to be shown
                    // TODO: fasten this process with use of sorting or not checking when we are active and before endTime of activity
                    if (c.seq.Length > 0)
                    {
						//check if we reached the next change in state
						if(c.nextTime<c.seq.Length && c.seq[c.nextTime]< watch.ElapsedMilliseconds/1000)
						{
							string[] marker_sequence= new string[1];

                            if (c.nextTime % 2 ==0) //nextTime is even -> start of flickering period
							{
								c.isActive= true;
								marker_sequence[0] = "Start "+c.name;
								c.nextTime++;
							}
							else //nextTime is odd -> end of the period
							{
								c.isActive= false;
                                //let the flicker become invisible
                                c.Screen.show(0);
								marker_sequence[0] = "End " + c.name;
                                c.nextTime++;
							}
							// Markers to specify the start and end of the flickering (following sequence)
							outl.push_sample(marker_sequence);
						}
                        if (c.isActive)
                        {
                            c.display();
                            c.index += 1 + lost_frame;
                        }
                    }
                    else //if no sequence for this flicker
                    {
                        c.display();
                        c.index += 1 + lost_frame;
                    }
				}
				);
				watchFPSMax.Stop();
				if (SDL.SDL_PollEvent(out evt) != 0)
				{
					if (evt.type == SDL.SDL_EventType.SDL_KEYUP && evt.key.keysym.sym == SDL.SDL_Keycode.SDLK_ESCAPE)
					{
						quit = true;
					}
					else
					{
						quit = false;
					}
				}
				// Remaining time for the frame after the display of all the flickers with the paralell loop
				var timeleft = frame_ticks - watchFPSMax.ElapsedTicks;
				//Console.WriteLine("Time rendering: {0} ms Total Time: {1} ms",watchFPSMax.ElapsedTicks/10000d, (timeleft + watchFPSMax.ElapsedTicks) / 10000d);
                Console.WriteLine("frame {0}: watch {1} ms left: {2} ms\nEstimated FPS: {3}\nEstimated Max Fps: {4}", frame,watch.ElapsedTicks/10000d,timeleft/10000d,1000d/((timeleft+watchFPSMax.ElapsedTicks)/10000d),10000000d/watchFPSMax.ElapsedTicks);
                

				// Wait until the full elapsed time for a frame
                if (timeleft > 0L) {
					if (timeleft > frame_ticks) { timeleft= frame_ticks; }
					Thread.Sleep((int)(timeleft / (TICKSPERSECONDS/1000d)));
					lost_frame= 0;
				} 
				else 
				{ 
					//# TODO: jump over frames in case of slow down, useful with sine flickers, breaks cVEP, but a slow down already breaks them anyway 
					Console.WriteLine("Warning Rendering can't keep up!! max FPS: {0}",frame*10000000d/watch.ElapsedTicks);
                    //left= Math.Abs(left);
                    //lost_frame = Convert.ToInt32(left / frame_ticks)+1;
                    //left -= (lost_frame-1) * frame_ticks;
                }
				//Console.WriteLine("Sleeped for: {0} ms, Asked: {1} ms",watchFPSMax.ElapsedMilliseconds,left/10000d);
				//Console.WriteLine("Current frame rate: {0} -> frameTicks : {1} ms", frameRate,frame_ticks/(TICKSPERSECONDS/1000d));
            }
			//Kill all Flickers Windows
			Console.WriteLine("Killing all Flickers");
			Parallel.ForEach<CFlicker>(m_listFlickers.Cast<CFlicker>(), c =>
            {
				c.isActive = false;
                c.Destroy();
            });
			SDL.SDL_Quit();
        }
		public void Close()
		{
			Environment.Exit(0);
		}
		public void Animate_Flicker(double time)
		{
            System.Timers.Timer timer = new System.Timers.Timer(time*1000)
            {
                AutoReset = false
            };
            timer.Elapsed += OnElapsed;
            timer.Start();
            void OnElapsed(object sender1, EventArgs e1)
            {
				quit=true;
            }
			try
			{
                Animate_Flicker();
            }catch(Exception ex)
			{
				Console.WriteLine(ex.Message);
                Parallel.ForEach<CFlicker>(m_listFlickers.Cast<CFlicker>(), c =>
                {
                    c.isActive = false;
                    c.Destroy();
                });
                SDL.SDL_Quit();
            }
        }
	}

}


