/*
	TUIO C# Demo - part of the reacTIVision project
	Copyright (c) 2005-2016 Martin Kaltenbrunner <martin@tuio.org>

	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
*/

using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using TUIO;
using System.IO;
using System.Drawing.Drawing2D;
public class TuioDemo : Form, TuioListener
{
	private TuioClient client;
	private Dictionary<long, TuioObject> objectList;
	private Dictionary<long, TuioCursor> cursorList;
	private Dictionary<long, TuioBlob> blobList;
	private TuioCursor lastCursor;  // Variable to store the last known cursor
	private TuioObject lastObject;

	public static int width, height;
	private int window_width = 640;
	private int window_height = 480;
	private int window_left = 0;
	private int window_top = 0;
	private int screen_width = Screen.PrimaryScreen.Bounds.Width;
	private int screen_height = Screen.PrimaryScreen.Bounds.Height;

	private bool fullscreen;
	private bool verbose;
	private int latestId;

	Font font = new Font("Arial", 10.0f);
	SolidBrush fntBrush = new SolidBrush(Color.White);
	SolidBrush bgrBrush = new SolidBrush(Color.FromArgb(0, 0, 64));
	SolidBrush curBrush = new SolidBrush(Color.FromArgb(192, 0, 192));
	SolidBrush objBrush = new SolidBrush(Color.FromArgb(64, 0, 0));
	SolidBrush blbBrush = new SolidBrush(Color.FromArgb(64, 64, 64));
	Pen curPen = new Pen(new SolidBrush(Color.Blue), 1);
    private string lastCursorText;
    private Image lastCursorImage;
    private string lastObjectText;
    private Image lastObjectImage;
    private Point lastObjectPosition = Point.Empty;

    public TuioDemo(int port) {

		verbose = false;
		fullscreen = false;
		width = window_width;
		height = window_height;

		this.ClientSize = new System.Drawing.Size(width, height);
		this.Name = "TuioDemo";
		this.Text = "TuioDemo";

		this.Closing += new CancelEventHandler(Form_Closing);
		this.KeyDown += new KeyEventHandler(Form_KeyDown);

		this.SetStyle(ControlStyles.AllPaintingInWmPaint |
						ControlStyles.UserPaint |
						ControlStyles.DoubleBuffer, true);

		objectList = new Dictionary<long, TuioObject>(128);
		cursorList = new Dictionary<long, TuioCursor>(128);
		blobList = new Dictionary<long, TuioBlob>(128);

		client = new TuioClient(port);
		client.addTuioListener(this);

		client.connect();
	}

	private void Form_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e) {

		if (e.KeyData == Keys.F1) {
			if (fullscreen == false) {

				width = screen_width;
				height = screen_height;

				window_left = this.Left;
				window_top = this.Top;

				this.FormBorderStyle = FormBorderStyle.None;
				this.Left = 0;
				this.Top = 0;
				this.Width = screen_width;
				this.Height = screen_height;

				fullscreen = true;
			} else {

				width = window_width;
				height = window_height;

				this.FormBorderStyle = FormBorderStyle.Sizable;
				this.Left = window_left;
				this.Top = window_top;
				this.Width = window_width;
				this.Height = window_height;

				fullscreen = false;
			}
		} else if (e.KeyData == Keys.Escape) {
			this.Close();

		} else if (e.KeyData == Keys.V) {
			verbose = !verbose;
		}

	}

	private void Form_Closing(object sender, System.ComponentModel.CancelEventArgs e)
	{
		client.removeTuioListener(this);

		client.disconnect();
		System.Environment.Exit(0);
	}

	public void addTuioObject(TuioObject o) {
		lock (objectList) {
			objectList.Add(o.SessionID, o);
		} if (verbose) Console.WriteLine("add obj " + o.SymbolID + " (" + o.SessionID + ") " + o.X + " " + o.Y + " " + o.Angle);
	}

	public void updateTuioObject(TuioObject o) {

		if (verbose) Console.WriteLine("set obj " + o.SymbolID + " " + o.SessionID + " " + o.X + " " + o.Y + " " + o.Angle + " " + o.MotionSpeed + " " + o.RotationSpeed + " " + o.MotionAccel + " " + o.RotationAccel);
	}

	public void removeTuioObject(TuioObject o) {
		lock (objectList) {
			objectList.Remove(o.SessionID);
		}
		if (verbose) Console.WriteLine("del obj " + o.SymbolID + " (" + o.SessionID + ")");
	}

	public void addTuioCursor(TuioCursor c) {
		lock (cursorList) {
			cursorList.Add(c.SessionID, c);
		}
		if (verbose) Console.WriteLine("add cur " + c.CursorID + " (" + c.SessionID + ") " + c.X + " " + c.Y);
	}

	public void updateTuioCursor(TuioCursor c) {
		if (verbose) Console.WriteLine("set cur " + c.CursorID + " (" + c.SessionID + ") " + c.X + " " + c.Y + " " + c.MotionSpeed + " " + c.MotionAccel);
	}

	public void removeTuioCursor(TuioCursor c) {
		lock (cursorList) {
			cursorList.Remove(c.SessionID);
		}
		if (verbose) Console.WriteLine("del cur " + c.CursorID + " (" + c.SessionID + ")");
	}

	public void addTuioBlob(TuioBlob b) {
		lock (blobList) {
			blobList.Add(b.SessionID, b);
		}
		if (verbose) Console.WriteLine("add blb " + b.BlobID + " (" + b.SessionID + ") " + b.X + " " + b.Y + " " + b.Angle + " " + b.Width + " " + b.Height + " " + b.Area);
	}

	public void updateTuioBlob(TuioBlob b) {

		if (verbose) Console.WriteLine("set blb " + b.BlobID + " (" + b.SessionID + ") " + b.X + " " + b.Y + " " + b.Angle + " " + b.Width + " " + b.Height + " " + b.Area + " " + b.MotionSpeed + " " + b.RotationSpeed + " " + b.MotionAccel + " " + b.RotationAccel);
	}

	public void removeTuioBlob(TuioBlob b) {
		lock (blobList) {
			blobList.Remove(b.SessionID);
		}
		if (verbose) Console.WriteLine("del blb " + b.BlobID + " (" + b.SessionID + ")");
	}

	public void refresh(TuioTime frameTime) {
		Invalidate();
	}

	private void DrawProductDetails(Graphics g, TuioObject tobj, PointF location, Color textColor)
	{
		// Sample product details (you can replace this with dynamic data)
		List<string> productDetails = new List<string>
	{
		"Brand: Dermatique",
		"Type: Purifying Cleansing Gel",
		"Volume: 150ml",
		"Features: Deep cleansing, suitable for all skin types",
		"Price: $15.99",
		"Usage: Apply a small amount to wet skin, massage, and rinse."
	};

		// Set the starting position for drawing the details
		float yOffset = 0;

		// Use the provided color for the text
		using (Brush textBrush = new SolidBrush(textColor))
		{
			foreach (var detail in productDetails)
			{
				g.DrawString($"• {detail}", font, textBrush, new PointF(location.X, location.Y + yOffset));
				yOffset += font.GetHeight(g) + 2; // Adjust vertical spacing between lines
			}
		}
	}


    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        // Getting the graphics object
        Graphics g = pevent.Graphics;

        // Draw the background image if it exists
        string backgroundImagePath = "background.jpg";
        if (File.Exists(backgroundImagePath))
        {
            using (Image bgImage = Image.FromFile(backgroundImagePath))
            {
                g.DrawImage(bgImage, new Rectangle(0, 0, width, height));
            }
        }

        // Draw the cursor path if available
        if (cursorList.Count > 0)
        {
            lock (cursorList)
            {
                foreach (TuioCursor tcur in cursorList.Values)
                {
                    List<TuioPoint> path = tcur.Path;
                    TuioPoint current_point = path[0];

                    for (int i = 1; i < path.Count; i++)
                    {
                        TuioPoint next_point = path[i];
                        g.DrawLine(curPen, current_point.getScreenX(width), current_point.getScreenY(height), next_point.getScreenX(width), next_point.getScreenY(height));
                        current_point = next_point;
                    }
                    g.FillEllipse(curBrush, current_point.getScreenX(width) - height / 100, current_point.getScreenY(height) - height / 100, height / 50, height / 50);
                    g.DrawString(tcur.CursorID.ToString(), font, fntBrush, new PointF(current_point.getScreenX(width) - 10, current_point.getScreenY(height) - 10));
                }
            }
        }

        // Draw the objects if they are detected
        if (objectList.Count > 0)
        {
            lock (objectList)
            {
                foreach (TuioObject tobj in objectList.Values)
                {
                    int ox = tobj.getScreenX(width);
                    int oy = tobj.getScreenY(height);
                    int size = height / 10;
                    latestId = tobj.SymbolID;

                    // Check if the TuioObject ID is 45 (Dermatique product)
                    if (tobj.SymbolID == 45)
                    {
                        try
                        {
                            // Load the dermatique image and make it transparent
                            Bitmap dermatiqueImage = new Bitmap("dermatique.jpg");
                            dermatiqueImage.MakeTransparent(dermatiqueImage.GetPixel(0, 0));
                            lastObjectImage = dermatiqueImage; // Store the image as the last known object image

                            int staticX = width / 2 - 80; // Static position for the image
                            int staticY = height / 2 - 100;
                            int newSize = size * 4;

                            g.DrawImage(lastObjectImage, new Rectangle(staticX - newSize / 2, staticY - newSize / 2, newSize, newSize));

                            // Draw product name and details at the static position
                            string displayText = "Dermatique Facial Wash";
                            Color textColor = Color.Green;
                            using (Brush textBrush = new SolidBrush(textColor))
                            {
                                g.DrawString(displayText, font, textBrush, new PointF(staticX - 20, staticY + newSize / 2 + 10));
                            }

                            // Draw product details at the static position
                            PointF detailsLocation = new PointF(staticX - 20, staticY + newSize / 2 + 30);
                            DrawProductDetails(g, tobj, detailsLocation, textColor);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error drawing object image: " + ex.Message);
                        }
                    }
                    else
                    {
                        // Display a square with the TUIO object ID for any other object
                        g.FillRectangle(Brushes.Blue, new Rectangle(ox - size / 2, oy - size / 2, size, size));
                        g.DrawString($"ID: {tobj.SymbolID}", font, Brushes.White, new PointF(ox - size / 2 + 5, oy - size / 2 + 5));

                        // Store the last known non-45 object ID and its position
                        lastObjectImage = null; // No image for non-45 objects
                        latestId = tobj.SymbolID;
                        lastObjectPosition = new Point(ox, oy);
                    }
                }
            }
        }

        // Draw the last known object image and details if no object is detected
        if (objectList.Count == 0)
        {
            if (latestId == 45 && lastObjectImage != null)
            {
                try
                {
                    int staticX = width / 2 - 80; // Static position for the last object
                    int staticY = height / 2 - 100;
                    int newSize = height / 10 * 4;

                    // Draw the last known object image
                    g.DrawImage(lastObjectImage, new Rectangle(staticX - newSize / 2, staticY - newSize / 2, newSize, newSize));

                    // Draw the product name and details
                    string lastDisplayText = "Dermatique Facial Wash";
                    Color lastTextColor = Color.Green;
                    using (Brush lastTextBrush = new SolidBrush(lastTextColor))
                    {
                        g.DrawString(lastDisplayText, font, lastTextBrush, new PointF(staticX - 20, staticY + newSize / 2 + 10));
                    }

                    // Draw product details at the static position
                    PointF lastDetailsLocation = new PointF(staticX - 20, staticY + newSize / 2 + 30);
                    DrawProductDetails(g, null, lastDetailsLocation, lastTextColor);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error drawing last known object image: " + ex.Message);
                }
            }
            else if (latestId != 0)
            {
                // Draw the last known square and object ID when it is not ID 45
                g.FillRectangle(Brushes.Blue, new Rectangle(lastObjectPosition.X - 30, lastObjectPosition.Y - 30, 60, 60));
                g.DrawString($"ID: {latestId}", font, Brushes.White, new PointF(lastObjectPosition.X - 25, lastObjectPosition.Y - 25));
            }
        }
    

    
	





        // Draw the blobs
        if (blobList.Count > 0)
		{
			lock (blobList)
			{
				foreach (TuioBlob tblb in blobList.Values)
				{
					int bx = tblb.getScreenX(width);
					int by = tblb.getScreenY(height);
					float bw = tblb.Width * width;
					float bh = tblb.Height * height;

					g.TranslateTransform(bx, by);
					g.RotateTransform((float)(tblb.Angle / Math.PI * 180.0f));
					g.TranslateTransform(-bx, -by);

					g.FillEllipse(blbBrush, bx - bw / 2, by - bh / 2, bw, bh);

					g.TranslateTransform(bx, by);
					g.RotateTransform(-1 * (float)(tblb.Angle / Math.PI * 180.0f));
					g.TranslateTransform(-bx, -by);

					g.DrawString(tblb.BlobID.ToString(), font, fntBrush, new PointF(bx, by));
				}
			}
		
}

// draw the blobs
if (blobList.Count > 0) {
				lock(blobList) {
					foreach (TuioBlob tblb in blobList.Values) {
						int bx = tblb.getScreenX(width);
						int by = tblb.getScreenY(height);
						float bw = tblb.Width*width;
						float bh = tblb.Height*height;

						g.TranslateTransform(bx, by);
						g.RotateTransform((float)(tblb.Angle / Math.PI * 180.0f));
						g.TranslateTransform(-bx, -by);

						g.FillEllipse(blbBrush, bx - bw / 2, by - bh / 2, bw, bh);

						g.TranslateTransform(bx, by);
						g.RotateTransform(-1 * (float)(tblb.Angle / Math.PI * 180.0f));
						g.TranslateTransform(-bx, -by);
						
						g.DrawString(tblb.BlobID + "", font, fntBrush, new PointF(bx, by));
					}
				}
			}
		}

    private void InitializeComponent()
    {
            this.SuspendLayout();
            // 
            // TuioDemo
            // 
            this.ClientSize = new System.Drawing.Size(282, 253);
            this.Name = "TuioDemo";
            this.Load += new System.EventHandler(this.TuioDemo_Load);
            this.ResumeLayout(false);

    }

    private void TuioDemo_Load(object sender, EventArgs e)
    {

    }

    public static void Main(String[] argv) {
	 		int port = 0;
			switch (argv.Length) {
				case 1:
					port = int.Parse(argv[0],null);
					if(port==0) goto default;
					break;
				case 0:
					port = 3333;
					break;
				default:
					Console.WriteLine("usage: mono TuioDemo [port]");
					System.Environment.Exit(0);
					break;
			}
			
			TuioDemo app = new TuioDemo(port);
			Application.Run(app);
		}
	}
