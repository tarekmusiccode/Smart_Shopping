using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading;
using TUIO;
using System.IO;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;
public class TuioDemo : Form, TuioListener
{
    private TuioClient client;
    private Dictionary<long, TuioObject> objectList;
    private Dictionary<long, TuioCursor> cursorList;
    private Dictionary<long, TuioBlob> blobList;
    private TuioCursor lastCursor;
    private TuioObject lastObject;
    private static TcpClient tcpClient;
    private static NetworkStream stream;
    private static CancellationTokenSource cts;
    public static int width, height;
    private int window_width = 640;
    private int window_height = 480;
    private int window_left = 0;
    private int window_top = 0;
    private int screen_width = Screen.PrimaryScreen.Bounds.Width;
    private int screen_height = Screen.PrimaryScreen.Bounds.Height;
    private bool fullscreen;
    private bool verbose;
    private Image lastObjectImage;
    private int latestId = -1;
    Font font = new Font("Arial", 10.0f);
    SolidBrush fntBrush = new SolidBrush(Color.White);
    SolidBrush bgrBrush = new SolidBrush(Color.FromArgb(0, 0, 64));
    SolidBrush curBrush = new SolidBrush(Color.FromArgb(192, 0, 192));
    SolidBrush objBrush = new SolidBrush(Color.FromArgb(64, 0, 0));
    SolidBrush blbBrush = new SolidBrush(Color.FromArgb(64, 64, 64));
    Pen curPen = new Pen(new SolidBrush(Color.Blue), 1);
    List<Tuple<Rectangle, Rectangle>> productRectangles = new List<Tuple<Rectangle, Rectangle>>();
    private Point lastObjectPosition = Point.Empty;
    List<string> imagePaths = new List<string> { "Sunblock.png", "dermatique1.png", "dermatique1.png" };
    string backgroundPath = "Untitled design (3).png";
    bool isAdmin = false;
    bool flagg = false;
    bool loggedIn = false;
    bool itemAdded = false;
    bool itemRemoved = false;
    bool flaglogin = false;
    List<string> knownNames = new List<string>
    {
        "Tarek", "Farah", "Youssef", "Malak", "Roqaia", "Rawan", "Unknown"
    };
    private Dictionary<string, string> ServerMessages = new Dictionary<string, string>
    {
        { "Gesture Program", null },
        { "Face Rec", null },
        { "object detection", null },

    };

    // admin
    Dictionary<string, int> stock = new Dictionary<string, int>
    {
        { "123", 6 },
        { "sunblock", 3 },
        { "dermatique", 7 }
    };
    //customer
    Dictionary<string, int> cart = new Dictionary<string, int>
    {
        { "123", 0 },
        { "sunblock", 0 },
        { "dermatique", 0 }
    };

    public TuioDemo(int port)
    {
        verbose = false;
        fullscreen = true;
        this.WindowState = FormWindowState.Maximized;
        width = Screen.PrimaryScreen.Bounds.Width;
        height = Screen.PrimaryScreen.Bounds.Height;
        this.ClientSize = new System.Drawing.Size(width, height);
        this.Name = "TuioDemo";
        this.Text = "TuioDemo";
        this.BackColor = Color.White;

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
        StartServerHandlers();
        cts = new CancellationTokenSource();
    }

    private void Form_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
    {
        if (e.KeyData == Keys.F1)
        {
            if (fullscreen == false)
            {
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
            }
            else
            {
                width = window_width;
                height = window_height;
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.Left = window_left;
                this.Top = window_top;
                this.Width = window_width;
                this.Height = window_height;
                fullscreen = false;
            }
        }
        else if (e.KeyData == Keys.Escape)
        {
            this.Close();
        }
        else if (e.KeyData == Keys.V)
        {
            verbose = !verbose;
        }
    }

    private void Form_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        client.removeTuioListener(this);
        client.disconnect();
        cts.Cancel();  // Stop listening for login
        tcpClient?.Close();  // Close TCP client if open
        System.Environment.Exit(0);
    }
    private bool latestIdIsFour = false;
    public void addTuioObject(TuioObject o)
    {
        lock (objectList)
        {
            objectList.Add(o.SessionID, o);
        }
        if (verbose) Console.WriteLine("add obj " + o.SymbolID + " (" + o.SessionID + ") " + o.X + " " + o.Y + " " + o.Angle);

        if (o.SymbolID == 4) // Check if SymbolID is 4
        {
            latestIdIsFour = true;
            Invalidate(); // Refresh GUI to show the login screen
        }
        else if (o.SymbolID == 1) // For SymbolID 1, show the admin screen
        {
            latestIdIsOne = true;
            Invalidate(); // Refresh GUI to show the admin menu
        }
    }

    public void updateTuioObject(TuioObject o)
    {
        if (verbose) Console.WriteLine("set obj " + o.SymbolID + " " + o.SessionID + " " + o.X + " " + o.Y + " " + o.Angle + " " + o.MotionSpeed + " " + o.RotationSpeed + " " + o.MotionAccel + " " + o.RotationAccel);
    }

    public void removeTuioObject(TuioObject o)
    {
        lock (objectList)
        {
            objectList.Remove(o.SessionID);
        }
        if (verbose) Console.WriteLine("del obj " + o.SymbolID + " (" + o.SessionID + ")");
        if (o.SymbolID == 4)
        {
            latestIdIsFour = false;
            Invalidate(); // Refresh GUI to hide the login screen
        }
        else if (o.SymbolID == 1)
        {
            latestIdIsOne = false;
            Invalidate(); // Refresh GUI to hide the admin menu
        }
    }

    public void addTuioCursor(TuioCursor c)
    {
        lock (cursorList)
        {
            cursorList.Add(c.SessionID, c);
        }
        if (verbose) Console.WriteLine("add cur " + c.CursorID + " (" + c.SessionID + ") " + c.X + " " + c.Y);
    }

    public void updateTuioCursor(TuioCursor c)
    {
        if (verbose) Console.WriteLine("set cur " + c.CursorID + " (" + c.SessionID + ") " + c.X + " " + c.Y + " " + c.MotionSpeed + " " + c.MotionAccel);
    }

    public void removeTuioCursor(TuioCursor c)
    {
        lock (cursorList)
        {
            cursorList.Remove(c.SessionID);
        }
        if (verbose) Console.WriteLine("del cur " + c.CursorID + " (" + c.SessionID + ")");
    }

    public void addTuioBlob(TuioBlob b)
    {
        lock (blobList)
        {
            blobList.Add(b.SessionID, b);
        }
        if (verbose) Console.WriteLine("add blb " + b.BlobID + " (" + b.SessionID + ") " + b.X + " " + b.Y + " " + b.Angle + " " + b.Width + " " + b.Height + " " + b.Area);
    }

    public void updateTuioBlob(TuioBlob b)
    {
        if (verbose) Console.WriteLine("set blb " + b.BlobID + " (" + b.SessionID + ") " + b.X + " " + b.Y + " " + b.Angle + " " + b.Width + " " + b.Height + " " + b.Area + " " + b.MotionSpeed + " " + b.RotationSpeed + " " + b.MotionAccel + " " + b.RotationAccel);
    }

    public void removeTuioBlob(TuioBlob b)
    {
        lock (blobList)
        {
            blobList.Remove(b.SessionID);
        }
        if (verbose) Console.WriteLine("del blb " + b.BlobID + " (" + b.SessionID + ")");
    }

    public void refresh(TuioTime frameTime)
    {
        Invalidate();
    }

    private void Stock(Graphics g, TuioObject tobj)
    {
        switch (isAdmin)
        {
            // admin
            case true:
                switch (tobj.SymbolID)
                {
                    case 3:
                        if (tobj.AngleDegrees > 30 && tobj.AngleDegrees < 90 && !itemAdded)
                        {
                            stock["sunblock"]++;
                            itemAdded = true;
                            itemRemoved = false;
                        }
                        else if (tobj.AngleDegrees > 270 && tobj.AngleDegrees < 310 && !itemRemoved && stock["sunblock"] > 0)
                        {
                            stock["sunblock"]--;
                            itemRemoved = true;
                            itemAdded = false;
                        }
                        else if (tobj.AngleDegrees <= 30 || tobj.AngleDegrees >= 310)
                        {
                            itemAdded = false;
                            itemRemoved = false;
                        }
                        break;
                    case 5:
                        if (tobj.AngleDegrees > 30 && tobj.AngleDegrees < 90 && !itemAdded)
                        {
                            stock["dermatique"]++;
                            itemAdded = true;
                            itemRemoved = false;
                        }
                        else if (tobj.AngleDegrees > 270 && tobj.AngleDegrees < 310 && !itemRemoved && stock["dermatique"] > 0)
                        {
                            stock["dermatique"]--;
                            itemRemoved = true;
                            itemAdded = false;
                        }
                        else if (tobj.AngleDegrees <= 30 || tobj.AngleDegrees >= 310)
                        {
                            itemAdded = false;
                            itemRemoved = false;
                        }
                        break;
                    case 6:
                        if (tobj.AngleDegrees > 30 && tobj.AngleDegrees < 90 && !itemAdded)
                        {
                            stock["123"]++;
                            itemAdded = true;
                            itemRemoved = false;
                        }
                        else if (tobj.AngleDegrees > 270 && tobj.AngleDegrees < 310 && !itemRemoved && stock["123"] > 0)
                        {
                            stock["123"]--;
                            itemRemoved = true;
                            itemAdded = false;
                        }
                        else if (tobj.AngleDegrees <= 30 || tobj.AngleDegrees >= 310)
                        {
                            itemAdded = false;
                            itemRemoved = false;
                        }
                        break;
                }
                break;
            //customer
            case false:

                break;
        }
    }
    private bool latestIdIsOne = false;
    private void drawmenu()
    {
        if (productRectangles.Count == 0)
        {
            int startX = 230;
            int spacing = 100;

            for (int i = 0; i < imagePaths.Count; i++)
            {
                if (File.Exists(imagePaths[i]))
                {
                    int totalHeight = 500;
                    int topOffset = (height - totalHeight) / 2;

                    Rectangle productRect = new Rectangle(startX, topOffset, 300, 450);
                    Rectangle bottomTextRect = new Rectangle(productRect.X, productRect.Bottom - 50, productRect.Width, 50);

                    productRectangles.Add(new Tuple<Rectangle, Rectangle>(productRect, bottomTextRect));

                    startX += productRect.Width + spacing;
                }
            }
        }



    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        bool isId3Present = false;
        bool isId17Present = false;
        drawmenu();
        Graphics g = pevent.Graphics;

        DrawHomePage(g);

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

        if (objectList.Count > 0)
        {
            lock (objectList)
            {
                foreach (TuioObject tobj in objectList.Values)
                {
                    if (tobj.SymbolID == 3) isId3Present = true;
                    if (tobj.SymbolID == 17) isId17Present = true;
                }
                foreach (TuioObject tobj in objectList.Values)
                {
                    if (tobj.SymbolID == 5) isId3Present = true;
                    if (tobj.SymbolID == 17) isId17Present = true;
                }
                foreach (TuioObject tobj in objectList.Values)
                {
                    if (tobj.SymbolID == 6) isId3Present = true;
                    if (tobj.SymbolID == 17) isId17Present = true;
                }
                foreach (TuioObject tobj in objectList.Values)
                {
                    int ox = tobj.getScreenX(width);
                    int oy = tobj.getScreenY(height);
                    int size = height / 10;
                    latestId = tobj.SymbolID;

                    if (tobj.SymbolID == 3 && isId17Present && flagg)
                    {
                        flaglogin = false;

                        DrawSunblockInfo(g);
                    }
                    else if (tobj.SymbolID == 3 && flagg)
                    {
                        flaglogin = false;

                        DrawMenuItems(g, selectedRectangleIndex: 0, flag: true, borderColor: Color.Orange);
                    }
                    else if (tobj.SymbolID == 2 && flagg)
                    {
                        flaglogin = false;

                        DrawMenuItems(g, selectedRectangleIndex: -1, flag: false, borderColor: Color.Transparent);
                    }
                    else if (tobj.SymbolID == 1 && flagg)
                    {
                        flaglogin = false;

                        ShowAdminMenu(g);
                    }
                    else if (tobj.SymbolID == 4)
                    {


                        flaglogin = true;
                        DrawLoginScreen(g);
                        latestId = 4;
                        // flagg = true;

                    }
                    else if (tobj.SymbolID == 5 && isId17Present && flagg)
                    {
                        flaglogin = false;

                        DrawDermatiqueinfo(g);
                    }
                    else if (tobj.SymbolID == 5 && flagg)
                    {
                        flaglogin = false;

                        DrawMenuItems(g, selectedRectangleIndex: 1, flag: true, borderColor: Color.Green);
                    }
                    else if (tobj.SymbolID == 6 && isId17Present && flagg)
                    {
                        flaglogin = false;

                        DrawSunblockInfo(g);
                    }
                    else if (tobj.SymbolID == 6 && flagg)
                    {
                        flaglogin = false;

                        DrawMenuItems(g, selectedRectangleIndex: 2, flag: true, borderColor: Color.Green);
                    }
                    else
                    {
                        g.FillRectangle(Brushes.Blue, new Rectangle(ox - size / 2, oy - size / 2, size, size));
                        g.DrawString($"ID: {tobj.SymbolID}", font, Brushes.White, new PointF(ox - size / 2 + 5, oy - size / 2 + 5));

                        lastObjectImage = null;
                        latestId = tobj.SymbolID;
                        lastObjectPosition = new Point(ox, oy);
                    }

                }

            }
        }

        if (objectList.Count == 0)
        {
            // MessageBox.Show("Received data:\n" + msg);
            if (latestId == 4)
            {
                DrawLoginScreen(g);
            }
            else if (latestId == 3 && isId17Present && flagg)
            {
                DrawSunblockInfo(g);
            }
            else if (latestId == 3 && !isId17Present && flagg)
            {
                DrawMenuItems(g, selectedRectangleIndex: -1, flag: false, borderColor: Color.Transparent);
            }
            else if (latestId == 2 && flagg)
            {
                DrawMenuItems(g, selectedRectangleIndex: -1, flag: false, borderColor: Color.Transparent);
            }
            else if (latestId == 5 && flagg)
            {
                DrawMenuItems(g, selectedRectangleIndex: -1, flag: false, borderColor: Color.Transparent);
            }
            else if (latestId == 6 && flagg)
            {
                DrawMenuItems(g, selectedRectangleIndex: -1, flag: false, borderColor: Color.Transparent);
            }
            else if (latestId == 1 && flagg)
            {
                ShowAdminMenu(g);
            }
            else if (latestId != 0)
            {
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

                    g.DrawString(tblb.BlobID + "", font, fntBrush, new PointF(bx, by));
                }
            }
        }
        // Display screens based on the latest detected object ID
        if (latestIdIsFour)
        {
            DrawLoginScreen(g);
        }
        if (latestIdIsOne)
        {
            ShowAdminMenu(g);
        }
    }
    private void DrawSunblockInfo(Graphics g)
    {
        try
        {
            if (File.Exists(backgroundPath))
            {
                using (Image backgroundImg = Image.FromFile(backgroundPath))
                {
                    g.DrawImage(backgroundImg, new Rectangle(0, 0, width, height));
                }
            }

            Rectangle rect = new Rectangle(150, 165, 400, 500);
            Color rectColor = Color.FromArgb(255, 234, 233, 239);
            int cornerRadius = 20;
            DrawRoundedRectangle(g, rect, rectColor, cornerRadius, Color.FromArgb(255, 255, 178, 34), 1);

            string[] sentences = {
    "Dermatique Sunblock: Broad-spectrum SPF protection.",
    "Lightweight, non-greasy, and quick-absorbing formula.",
    "Hydrates skin while shielding from UV damage.",
    "Ideal for daily use on all skin types."
};

            Rectangle rect2 = new Rectangle(150, 165, 400, 500);
            Font textFont = new Font("Arial", 14, FontStyle.Regular);
            Brush textBrush = new SolidBrush(Color.FromArgb(100, 100, 100));
            int verticalPadding = 95;
            int horizontalPadding = 20;
            int lineSpacing = 20;
            DrawTextBlock(g, rect2, sentences, textFont, textBrush, verticalPadding, horizontalPadding, lineSpacing);

            if (File.Exists(imagePaths[0]))
            {
                using (Image img = Image.FromFile(imagePaths[0]))
                {
                    g.DrawImage(img, new Rectangle(width - 600, 50, 400, 600));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error drawing sunblock info: " + ex.Message);
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

    private void ShowUserScreen(Graphics g, string user = "", string name = "")
    {
        try
        {
            using (Image bgImage = Image.FromFile("plain_bk.png"))
            {
                g.DrawImage(bgImage, new Rectangle(0, 0, width, height));
            }

            int textStartX = width / 2 - 150;
            int textStartY = height / 2 - 150;

            using (Font textFont = new Font("Verdana", 20, FontStyle.Bold))
            using (SolidBrush textBrush = new SolidBrush(Color.Black))
            {
                switch (user)
                {
                    case "admin":
                        g.DrawString("Welcome Admin!", textFont, textBrush, textStartX, textStartY);
                        textStartY += 30;
                        g.DrawString("Show the product on the screen", textFont, textBrush, textStartX, textStartY);
                        break;
                    case "customer":
                        g.DrawString("Welcome Customer, happy shopping!", textFont, textBrush, textStartX, textStartY);
                        textStartY += 30;
                        g.DrawString("Show the product on the screen", textFont, textBrush, textStartX, textStartY);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error drawing user screen: {ex.Message}");
        }
    }
    private void DrawMenuItems(Graphics g, int selectedRectangleIndex, bool flag, Color borderColor)
    {
        try
        {
            // Draw the background image if it exists
            if (File.Exists(backgroundPath))
            {
                using (Image backgroundImg = Image.FromFile(backgroundPath))
                {
                    g.DrawImage(backgroundImg, new Rectangle(0, 0, width, height));
                }
            }

            // Loop through each product rectangle to draw product details
            for (int i = 0; i < productRectangles.Count; i++)
            {
                // Draw the main product display rectangle without rounded corners
                Rectangle productRect = productRectangles[i].Item1;
                Color productRectColor = Color.FromArgb(255, 234, 233, 239);

                // Draw rectangle with border if selected, else no border
                bool drawBorder = (flag && i == selectedRectangleIndex);
                Color effectiveBorderColor = drawBorder ? borderColor : productRectColor;
                int borderWidth = drawBorder ? 3 : 0; // Adjust width if selected

                DrawRectangleBorder(g, productRect, productRectColor, effectiveBorderColor, borderWidth, drawBorder);

                // Draw the product image inside the rectangle without changing its location
                if (File.Exists(imagePaths[i]))
                {
                    using (Image productImg = Image.FromFile(imagePaths[i]))
                    {
                        int imgX, imgY, imgWidth, imgHeight;

                        if (i == 0)
                        {
                            imgX = (productRect.X + (productRect.Width - 200) / 2) - 40;
                            imgY = productRect.Y - 20;
                            imgWidth = 280;
                            imgHeight = 400;
                        }
                        else
                        {
                            imgX = productRect.X + (productRect.Width - 200) / 2;
                            imgY = productRect.Y + 20;
                            imgWidth = 200;
                            imgHeight = 350;
                        }

                        g.DrawImage(productImg, new Rectangle(imgX, imgY, imgWidth, imgHeight));
                    }
                }

                // Set text based on index
                string bottomText = i == 0 ? "Sunblock" : "Cleansing Gel";

                // Draw the pink section at the bottom
                Rectangle bottomTextRect = productRectangles[i].Item2;
                using (SolidBrush pinkBrush = new SolidBrush(Color.FromArgb(255, 254, 81, 161)))
                {
                    g.FillRectangle(pinkBrush, bottomTextRect);
                }

                // Draw text inside the pink section
                using (Font textFont = new Font("Arial", 14, FontStyle.Bold))
                using (Brush textBrush = new SolidBrush(Color.White))
                {
                    SizeF textSize = g.MeasureString(bottomText, textFont);
                    PointF textLocation = new PointF(
                        bottomTextRect.X + (bottomTextRect.Width - textSize.Width) / 2,
                        bottomTextRect.Y + (bottomTextRect.Height - textSize.Height) / 23
                    );
                    g.DrawString(bottomText, textFont, textBrush, textLocation);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error drawing product display: " + ex.Message);
        }
    }
    private void DrawRectangleBorder(Graphics g, Rectangle rect, Color rectColor, Color borderColor, int borderWidth, bool drawBorder)
    {
        // Fill the rectangle with the background color
        using (SolidBrush brush = new SolidBrush(rectColor))
        {
            g.FillRectangle(brush, rect);
        }

        // Draw the border if needed
        if (drawBorder)
        {
            using (Pen pen = new Pen(borderColor, borderWidth))
            {
                g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
            }
        }
    }
    private void ShowAdminMenu(Graphics g)
    {
        try
        {
            // Define the menu background
            Color menuBackgroundColor = Color.FromArgb(255, 233, 235, 239);
            Rectangle menuRect = new Rectangle(50, 50, width - 100, height - 100);
            g.FillRectangle(new SolidBrush(menuBackgroundColor), menuRect);

            // Define button properties
            int buttonWidth = 250;
            int buttonHeight = 60;
            int buttonSpacing = 30;
            int startX = (width - buttonWidth) / 2;
            int startY = (height - 3 * buttonHeight - 2 * buttonSpacing) / 2;

            // Option 1: Update Stock
            Rectangle updateStockButtonRect = new Rectangle(startX, startY, buttonWidth, buttonHeight);
            DrawRoundedButton(g, updateStockButtonRect, "Update Stock");

            // Option 2: Remove Product
            Rectangle removeProductButtonRect = new Rectangle(startX, startY + buttonHeight + buttonSpacing, buttonWidth, buttonHeight);
            DrawRoundedButton(g, removeProductButtonRect, "Remove Product");



            // Draw instructions or header
            string headerText = "Admin Menu";
            using (Font headerFont = new Font("Arial", 18, FontStyle.Bold))
            using (Brush headerBrush = new SolidBrush(Color.Black))
            {
                SizeF headerSize = g.MeasureString(headerText, headerFont);
                PointF headerLocation = new PointF((width - headerSize.Width) / 2, startY - headerSize.Height - 20);
                g.DrawString(headerText, headerFont, headerBrush, headerLocation);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error drawing admin menu: " + ex.Message);
        }
    }
    private void DrawDermatiqueinfo(Graphics g)
    {
        try
        {
            if (File.Exists(backgroundPath))
            {
                using (Image backgroundImg = Image.FromFile(backgroundPath))
                {
                    g.DrawImage(backgroundImg, new Rectangle(0, 0, width, height));
                }
            }

            Rectangle rect = new Rectangle(150, 165, 400, 500);
            Color rectColor = Color.FromArgb(255, 234, 233, 239);
            int cornerRadius = 20;
            DrawRoundedRectangle(g, rect, rectColor, cornerRadius, Color.Green, 1);
            string[] sentences = {
    "Dermatique Facial Wash: Daily purifying formula.",
    "Botanicals cleanse deeply without drying.",
    "Suitable for all skin types, including sensitive.",
    "Use: Apply to damp skin, massage, and rinse."
};
            Rectangle rect2 = new Rectangle(150, 165, 400, 500);
            Font textFont = new Font("Arial", 14, FontStyle.Regular);
            Brush textBrush = new SolidBrush(Color.FromArgb(100, 100, 100));
            int verticalPadding = 95;
            int horizontalPadding = 20;
            int lineSpacing = 20;
            DrawTextBlock(g, rect2, sentences, textFont, textBrush, verticalPadding, horizontalPadding, lineSpacing);

            if (File.Exists(imagePaths[1]))
            {
                using (Image img = Image.FromFile(imagePaths[1]))
                {
                    g.DrawImage(img, new Rectangle(width - 600, 50, 400, 600));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error drawing sunblock info: " + ex.Message);
        }
    }
    public void DrawTextBlock(Graphics g, Rectangle rect, string[] sentences, Font textFont, Brush textBrush, int verticalPadding, int horizontalPadding, int lineSpacing)
    {
        float startY = rect.Y + verticalPadding;
        float maxWidth = rect.Width - (2 * horizontalPadding);
        float lineHeight = textFont.GetHeight(g) * 3;

        StringFormat format = new StringFormat();
        format.Alignment = StringAlignment.Near;
        format.LineAlignment = StringAlignment.Near;
        format.FormatFlags = StringFormatFlags.LineLimit;

        foreach (string sentence in sentences)
        {
            g.DrawString(sentence, textFont, textBrush, new RectangleF(rect.X + horizontalPadding, startY, maxWidth, lineHeight), format);
            startY += lineHeight + lineSpacing;
        }
    }
    private void DrawProductPage(Graphics g, string productName, string productImagePath, string backgroundImagePath)
    {
        try
        {
            if (!File.Exists(productImagePath))
            {
                throw new Exception($"No such product image named {productImagePath}");
            }
            if (!File.Exists(backgroundImagePath))
            {
                throw new Exception($"No such background image named {backgroundImagePath}");
            }
            using (Image bgImage = Image.FromFile(backgroundImagePath))
            {
                g.DrawImage(bgImage, new Rectangle(0, 0, width, height));
            }
            List<string> productText = new List<string>();
            Image img = Image.FromFile(productImagePath);
            Font font = new Font("Verdana", 15);
            SolidBrush brush = new SolidBrush(Color.Black);
            float x = 0, y = 0;
            switch (productName)
            {
                case "sunblock":
                    productText = new List<string>() {
                        "Dermatique Sunblock: Broad-spectrum SPF protection.",
                        "Lightweight, non-greasy, and quick-absorbing formula.",
                        "Hydrates skin while shielding from UV damage.",
                        "Ideal for daily use on all skin types."
                    };
                    g.DrawImage(img, new Rectangle((width / 2) - 400, (height / 2) - 600, 400, 600));
                    x = width / 4;
                    y = (float)(height / 1.5);
                    foreach (string text in productText)
                    {
                        g.DrawString(text, font, brush, new PointF(x, y));
                        y += 20;
                    }
                    g.DrawString($"In-Stock: {stock["sunblock"]}", font, brush, new Point(30, 30));
                    if (!isAdmin) g.DrawString($"Your cart: {cart["sunblock"]}", font, brush, new Point(60, 30));
                    break;
                case "dermatique":
                    productText = new List<string>() {
                        "Brand: Dermatique",
                        "Type: Purifying Cleansing Gel",
                        "Volume: 150ml",
                        "Features: Deep cleansing, suitable for all skin types",
                        "Price: $15.99",
                        "Usage: Apply a small amount to wet skin, massage, and rinse."
                    };
                    g.DrawImage(img, new Rectangle((width / 2) - 400, (height / 2) - 600, 400, 600));
                    x = width / 4;
                    y = (float)(height / 1.5);
                    foreach (string text in productText)
                    {
                        g.DrawString(text, font, brush, new PointF(x, y));
                        y += 20;
                    }
                    g.DrawString($"In-Stock: {stock["dermatique"]}", font, brush, new Point(30, 30));
                    if (!isAdmin) g.DrawString($"Your cart: {cart["dermatique"]}", font, brush, new Point(60, 30));
                    break;
                case "123":
                    productText = new List<string>() {
                        "Dermatique Facial Wash: Daily purifying formula.",
                        "Botanicals cleanse deeply without drying.",
                        "Suitable for all skin types, including sensitive.",
                        "Use: Apply to damp skin, massage, and rinse."
                    };
                    g.DrawImage(img, new Rectangle((width / 2) - 400, (height / 2) - 600, img.Width / 2, img.Height / 2));
                    x = width / 4;
                    y = (float)(height / 1.5);
                    foreach (string text in productText)
                    {
                        g.DrawString(text, font, brush, new PointF(x, y));
                        y += 20;
                    }
                    g.DrawString($"In-Stock: {stock["123"]}", font, brush, new Point(30, 30));
                    if (!isAdmin) g.DrawString($"Your cart: {cart["123"]}", font, brush, new Point(60, 30));
                    break;
            }
            using (Font helpFont = new Font("Verdana", 18, FontStyle.Bold))
            using (SolidBrush helpBrush = new SolidBrush(Color.Gray))
            {
                string helpText = "For instructions, show Marker 7";
                g.DrawString(helpText, helpFont, helpBrush, width - 450, 10);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occured loading images: {ex.Message}");
        }
    }
    private void DrawRoundedRectangle(Graphics g, Rectangle rect, Color rectColor, int cornerRadius, Color borderColor, int flag)
    {
        using (GraphicsPath path = new GraphicsPath())
        {
            int diameter = cornerRadius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            // Fill the rounded rectangle
            using (SolidBrush brush = new SolidBrush(rectColor))
            {
                g.FillPath(brush, path);
            }

            // Draw a border if flag is set
            if (flag == 1)
            {
                using (Pen pen = new Pen(borderColor, 3))
                {
                    g.DrawPath(pen, path);
                }
            }
        }
    }

    private void DrawHomePage(Graphics g)
    {
        int start = (height / 3) - 30;
        string backgroundImagePath = "home.png";
        if (File.Exists(backgroundImagePath))
        {
            using (Image bgImage = Image.FromFile(backgroundImagePath))
            {
                g.DrawImage(bgImage, new Rectangle(0, 0, width, height));
            }
        }

        using (Font titleFont = new Font("Verdana", 24, FontStyle.Bold))
        using (SolidBrush titleBrush = new SolidBrush(Color.Purple))
        {
            g.DrawString("Smart Pharmacy Shop", titleFont, titleBrush, new Point(50, start));
        }
        start += 80;

        using (Font descFont = new Font("Verdana", 12))
        using (SolidBrush descBrush = new SolidBrush(Color.Gray))
        {
            g.DrawString("Welcome!", descFont, descBrush, new RectangleF(50, start, 700, 50));
            start += 30;
            g.DrawString("To get started,", descFont, descBrush, new RectangleF(50, start, 700, 50));
            start += 30;
            g.DrawString("grab any medicine and display it infront of the camera", descFont, descBrush, new RectangleF(50, start, 700, 30));
            start += 30;
            g.DrawString("to show the information of the medicine", descFont, descBrush, new RectangleF(50, start, 700, 30));
            start += 30;
            g.DrawString("First to proceed, Show marker 0", descFont, descBrush, new RectangleF(50, start, 700, 30));
        }
        start += 80;

        Rectangle buttonRect = new Rectangle(50, start, 180, 40);
        using (GraphicsPath path = new GraphicsPath())
        {
            path.AddArc(buttonRect.X, buttonRect.Y, 40, 40, 180, 90);
            path.AddArc(buttonRect.X + buttonRect.Width - 40, buttonRect.Y, 40, 40, 270, 90);
            path.AddArc(buttonRect.X + buttonRect.Width - 40, buttonRect.Y + buttonRect.Height - 40, 40, 40, 0, 90);
            path.AddArc(buttonRect.X, buttonRect.Y + buttonRect.Height - 40, 40, 40, 90, 90);
            path.CloseAllFigures();

            using (SolidBrush buttonBrush = new SolidBrush(Color.Purple))
            {
                g.FillPath(buttonBrush, path);
            }
            using (Pen borderPen = new Pen(Color.DarkMagenta, 6))
            {
                g.DrawPath(borderPen, path);
            }
            using (Font buttonFont = new Font("Verdana", 16, FontStyle.Bold))
            using (SolidBrush buttonTextBrush = new SolidBrush(Color.White))
            {
                SizeF textSize = g.MeasureString("Shop Now", buttonFont);
                PointF textLocation = new PointF(
                    buttonRect.X + (buttonRect.Width - textSize.Width + 10) / 2,
                    buttonRect.Y + (buttonRect.Height - textSize.Height) / 2
                );
                g.DrawString("Shop Now", buttonFont, buttonTextBrush, textLocation);
            }
        }
    }

    private void DrawLoginScreen(Graphics g)
    {
        try
        {
            g.Clear(Color.White);

            string loginBackgroundPath = "bk login.jpg"; // Replace with actual image path
            if (File.Exists(loginBackgroundPath))
            {
                using (Image loginBackground = Image.FromFile(loginBackgroundPath))
                {
                    g.DrawImage(loginBackground, new Rectangle(0, 0, width, height));
                }
            }

            Rectangle customerButtonRect = new Rectangle(1060, height / 2, 200, 50);
            DrawRoundedButton(g, customerButtonRect, "Login as Customer");

            Rectangle adminButtonRect = new Rectangle(1260, height / 2, 200, 50);
            DrawRoundedButton(g, adminButtonRect, "Login as Admin");

            string open = "Please open your bluetooth";
            Font openFont = new Font("Arial", 12, FontStyle.Italic | FontStyle.Bold);
            Brush openBrush = new SolidBrush(Color.DarkSlateBlue);

            PointF openPosition = new PointF(
                customerButtonRect.X + (customerButtonRect.Width / 2) - (g.MeasureString(open, openFont).Width / 2), // Centered under the button
                customerButtonRect.Bottom + 10
            );
            g.DrawString(open, openFont, openBrush, openPosition);
            string loginImagePath = "login image.png";
            if (File.Exists(loginImagePath))
            {
                using (Bitmap loginImage = new Bitmap(loginImagePath))
                {
                    g.DrawImage(loginImage, new Rectangle(0, 0, width / 2, height));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error drawing login screen: " + ex.Message);
        }

    }

    private void DrawRoundedButton(Graphics g, Rectangle buttonRect, string buttonText)
    {
        using (GraphicsPath path = new GraphicsPath())
        {
            int cornerRadius = 20;
            path.AddArc(buttonRect.X, buttonRect.Y, cornerRadius, cornerRadius, 180, 90);
            path.AddArc(buttonRect.X + buttonRect.Width - cornerRadius, buttonRect.Y, cornerRadius, cornerRadius, 270, 90);
            path.AddArc(buttonRect.X + buttonRect.Width - cornerRadius, buttonRect.Y + buttonRect.Height - cornerRadius, cornerRadius, cornerRadius, 0, 90);
            path.AddArc(buttonRect.X, buttonRect.Y + buttonRect.Height - cornerRadius, cornerRadius, cornerRadius, 90, 90);
            path.CloseAllFigures();

            // Fill button background
            using (SolidBrush buttonBrush = new SolidBrush(Color.FromArgb(0, 180, 216)))
            {
                g.FillPath(buttonBrush, path);
            }

            // Draw button border
            using (Pen borderPen = new Pen(Color.White, 2))
            {
                g.DrawPath(borderPen, path);
            }

            // Draw button text
            using (Font buttonFont = new Font("Arial", 12, FontStyle.Bold))
            using (SolidBrush buttonTextBrush = new SolidBrush(Color.White))
            {
                SizeF textSize = g.MeasureString(buttonText, buttonFont);
                PointF textLocation = new PointF(
                    buttonRect.X + (buttonRect.Width - textSize.Width) / 2,
                    buttonRect.Y + (buttonRect.Height - textSize.Height) / 2
                );
                g.DrawString(buttonText, buttonFont, buttonTextBrush, textLocation);
            }
        }
    }

    private void Tutorial(Graphics g)
    {
        using (Image bgImage = Image.FromFile("orangee.png"))
        {
            g.DrawImage(bgImage, new Rectangle(0, 0, width, height));
        }
        int textStartX = 50;
        int textStartY = height / 2 - 150;

        using (Font textFont = new Font("Verdana", 14, FontStyle.Bold))
        using (SolidBrush textBrush = new SolidBrush(Color.Black))
        {

            g.DrawString("To know the details of the product you are holding", textFont, textBrush, textStartX, textStartY);
            textStartY += 30;
            g.DrawString(" show it to the camera making sure the black drawing(marker) is shown", textFont, textBrush, textStartX, textStartY);

            textStartY += 60;

            if (isAdmin)
            {
                g.DrawString("To increase stock quantity of your product in the pharmacy shop,", textFont, textBrush, textStartX, textStartY);
                textStartY += 30;
                g.DrawString("rotate product clockwise direction,", textFont, textBrush, textStartX, textStartY);
                textStartY += 30;
                g.DrawString("to decrease, rotate product anticlockwise direction", textFont, textBrush, textStartX, textStartY);
            }
            else
            {
                g.DrawString("To increase quantity of your product in the shopping cart,", textFont, textBrush, textStartX, textStartY);
                textStartY += 30;
                g.DrawString("rotate product clockwise direction,", textFont, textBrush, textStartX, textStartY);
                textStartY += 30;
                g.DrawString("to decrease, rotate product anticlockwise direction", textFont, textBrush, textStartX, textStartY);
            }

            textStartY += 60;

            g.DrawString("To confirm your selection, show marker 8", textFont, textBrush, textStartX, textStartY);

            textStartY += 60;
            g.DrawString("To close this screen, show the product again", textFont, textBrush, textStartX, textStartY);
        }
    }


    private async Task StartListeningForLogin(CancellationToken token)
    {
        try
        {
            TcpListener listener = new TcpListener(System.Net.IPAddress.Parse("127.0.0.1"), 5000);
            listener.Start();

            while (!token.IsCancellationRequested && !messageDisplayed) // Stop listening if a message has been displayed
            {
                if (listener.Pending())
                {
                    using (TcpClient client = await listener.AcceptTcpClientAsync())
                    using (NetworkStream stream = client.GetStream())
                    {
                        byte[] buffer = new byte[1024];
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                        if (bytesRead > 0)
                        {
                            string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                            ProcessLoginMessage(message);
                        }
                    }
                }

                await Task.Delay(10);  // Prevent tight loop
            }
            listener.Stop();
        }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested)
            {
                Console.WriteLine("Error in TCP connection: " + ex.Message);
            }
        }
    }

    private bool messageDisplayed = false; // Flag to track if a message has been displayed

    // Show message only if TUIO object 4 is present and no message has been displayed
    private async void ProcessLoginMessage(string message)
    {
        if (latestIdIsFour && !messageDisplayed)
        {
            messageDisplayed = true;

            this.Invoke(new Action(() =>
            {
                ShowMessageOnScreen("Loading... Searching for user", Color.Orange);
            }));

            // Wait for 5 seconds
            await Task.Delay(5000);

            // Now show the actual message based on login or registration status
            this.Invoke(new Action(() =>
            {
                string displayMessage = message == "User registered" ? "A new user has been registered." : "User has successfully logged in.";
                Color messageColor = message == "User registered" ? Color.Green : Color.Blue;

                ShowMessageOnScreen(displayMessage, messageColor); // Show the final message
                MessageBox.Show(displayMessage, message == "User registered" ? "Registration" : "Login", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }));
        }
    }

    private void ShowMessageOnScreen(string text, Color color)
    {
        Label messageLabel = new Label
        {
            Text = text,
            ForeColor = color,
            Font = new Font("Arial", 18, FontStyle.Bold),
            AutoSize = true,
            BackColor = Color.Transparent,
            Location = new Point((width - 300) / 2, (height - 50) / 2)
        };

        this.Controls.Clear(); // Clear previous controls
        this.Controls.Add(messageLabel);
    }


    public static void Main(String[] argv)
    {
        int port = 0;
        switch (argv.Length)
        {
            case 1:
                port = int.Parse(argv[0], null);
                if (port == 0) goto default;
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

    private void StartServerHandlers()
    {
        string server = "localhost";
        int port = 5010;

        // Only one connection to the server, but multiple handlers for different services
        Task.Run(() => HandleServerAsync(server, port, "Gesture Program"));
        Task.Run(() => HandleServerAsync(server, port, "Face Recognition"));
        Task.Run(() => HandleServerAsync(server, port, "object Detection"));
    }

    private async Task HandleServerAsync(string server, int port, string serverName)
    {
        try
        {
            using (TcpClient client = new TcpClient())
            {
                MessageBox.Show($"{serverName}: Attempting to connect to {server}:{port}...");

                // Wait for 3 seconds before trying to connect to ensure the server is ready
                await Task.Delay(3000); // Wait a little longer before trying to connect

                await client.ConnectAsync(server, port);  // Connect to the Python server

                MessageBox.Show($"{serverName}: Connected!");

                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[1024];
                    while (true)
                    {
                        if (stream.DataAvailable)
                        {
                            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                            if (bytesRead > 0)
                            {
                                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                                MessageBox.Show($"{serverName}: Received -> {message}");
                            }
                        }
                        await Task.Delay(100);  // Delay to reduce CPU usage
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"{serverName}: Error - {ex.Message}");
        }
    }


}