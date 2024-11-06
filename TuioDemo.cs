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
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Text;


using System.Drawing.Text;
public class TuioDemo : Form, TuioListener
{
    private TcpClient serverClient;
    private NetworkStream serverStream;
    private string serverHost = "127.0.0.1";
    private int serverPort = 5010;

    private TuioClient client;

    private Dictionary<long, TuioObject> objectList;
    private Dictionary<long, TuioCursor> cursorList;
    private Dictionary<long, TuioBlob> blobList;
    private TuioCursor lastCursor;  // Variable to store the last known cursor
    private TuioObject lastObject;
    private static TcpClient tcpClient;
    private static NetworkStream stream;
    private static CancellationTokenSource cts;
    private bool latestIdIsFour = false;

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
    List<string> imagePaths = new List<string> { "Sunblock.png", "dermatique1.png", "dermatique1.png" };
    int imageStartX = width - 100;
    int imageStartY = 150;
    int imageWidth = 100;
    int imageHeight = 100;
    List<Tuple<Rectangle, Rectangle>> productRectangles = new List<Tuple<Rectangle, Rectangle>>();
    string backgroundPath = "Untitled design (3).png";
    string login_background = "bk login.jpg";
    public TuioDemo(int port)
    {

        verbose = false;
        fullscreen = true;

        // Set the form to be borderless and maximized
        //this.FormBorderStyle = FormBorderStyle.None;
        this.WindowState = FormWindowState.Maximized;

        // Set ClientSize to match screen resolution
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
         ConnectToServer();
       // StartServerListener();

        //  cts = new CancellationTokenSource();
        //Task.Run(() => StartListeningForLogin(cts.Token));
    }
    private TcpListener serverListener;

private async void StartServerListener()
{
    try
    {
        serverListener = new TcpListener(System.Net.IPAddress.Parse(serverHost), serverPort);
        serverListener.Start();
        Console.WriteLine("Server is listening on " + serverHost + ":" + serverPort);

        while (true)
        {
            TcpClient client = await serverListener.AcceptTcpClientAsync();
            Console.WriteLine("Client connected.");
            ProcessClient(client);
        }
    }
    catch (SocketException e)
    {
        MessageBox.Show("SocketException: " + e.Message);
    }
}

private async void ProcessClient(TcpClient client)
{
    try
    {
        using (NetworkStream stream = client.GetStream())
        {
            byte[] buffer = new byte[1024];
            StringBuilder completeMessage = new StringBuilder();
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                completeMessage.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
                string receivedData = completeMessage.ToString();
                if (!string.IsNullOrEmpty(receivedData))
                {
                    MessageBox.Show("Received data:\n" + receivedData);
                }
                completeMessage.Clear();
            }
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show("Exception: " + ex.Message);
    }
}

    private async void ConnectToServer()
    {
        try
        {
            serverClient = new TcpClient(serverHost, serverPort);
            serverStream = serverClient.GetStream();
            byte[] buffer = new byte[1024];
            StringBuilder completeMessage = new StringBuilder();
            int bytesRead;

            // Read asynchronously to avoid blocking the UI thread
            while ((bytesRead = await serverStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                completeMessage.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));

                // Show the message box immediately when data is received
                string receivedData = completeMessage.ToString();
                if (!string.IsNullOrEmpty(receivedData))
                {
                    // Display received data in a message box
                    MessageBox.Show("Received data:\n" + receivedData);
                }

                completeMessage.Clear(); // Clear after showing the message
            }
        }
        catch (SocketException e)
        {
            MessageBox.Show("SocketException: " + e.Message);
        }
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
        //cts.Cancel();  // Stop listening for login
        //tcpClient?.Close();  // Close TCP client if open
        System.Environment.Exit(0);
    }

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
    private void drawmenu()
    {
        if (productRectangles.Count == 0) // Only initialize if not already populated
        {
            int startX = 230;
            int spacing = 100;

            for (int i = 0; i < imagePaths.Count; i++)
            {
                if (File.Exists(imagePaths[i]))
                {
                    // Calculate space for centering
                    int totalHeight = 500; // Total height for product rectangle and pink section
                    int topOffset = (height - totalHeight) / 2; // Center vertically based on available space

                    // Create the main product display rectangle
                    Rectangle productRect = new Rectangle(startX, topOffset, 300, 450); // Adjusted for desired width and height
                    Rectangle bottomTextRect = new Rectangle(productRect.X, productRect.Bottom - 50, productRect.Width, 50); // Pink section below product

                    // Store the rectangles in the list
                    productRectangles.Add(new Tuple<Rectangle, Rectangle>(productRect, bottomTextRect));

                    // Increment X for next product rectangle
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
        // Getting the graphics object
        Graphics g = pevent.Graphics;

        DrawHomePage(g);

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
                    if (tobj.SymbolID == 3) isId3Present = true;
                    if (tobj.SymbolID == 17) isId17Present = true;
                }
                foreach (TuioObject tobj in objectList.Values)
                {
                    int ox = tobj.getScreenX(width);
                    int oy = tobj.getScreenY(height);
                    int size = height / 10;
                    latestId = tobj.SymbolID;

                    if (tobj.SymbolID == 3 && isId17Present)
                    {
                        DrawSunblockInfo(g);
                    }
                    else if (tobj.SymbolID == 3) //to show info of dermatique sunblock
                    {
                        DrawMenuItems(g, selectedRectangleIndex: 0, flag: true, borderColor: Color.Orange);

                    }
                    else if (tobj.SymbolID == 2)
                    {
                        DrawMenuItems(g, selectedRectangleIndex: -1, flag: false, borderColor: Color.Transparent);
                    }

                    else if (tobj.SymbolID == 4)
                    {


                        DrawLoginScreen(g);
                       // StartServerListener();
                        latestId = 4;
                    }
                    else if (tobj.SymbolID == 5)
                    {
                        DrawMenuItems(g, selectedRectangleIndex: 1, flag: true, borderColor: Color.Green);
                    }
                    else if (tobj.SymbolID == 6)
                    {
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

        // Draw the last known object image and details if no object is detected
        if (objectList.Count == 0)
        {

            if (latestId == 4)
            {
                DrawLoginScreen(g);

            }
            else if (latestId == 3 && isId17Present)
            {
                DrawSunblockInfo(g);
            }

            else if (latestId == 3 && !isId17Present)
            {
                DrawMenuItems(g, selectedRectangleIndex: -1, flag: false, borderColor: Color.Transparent);
            }
            else if (latestId == 2)
            {
                DrawMenuItems(g, selectedRectangleIndex: -1, flag: false, borderColor: Color.Transparent);
            }
            else if (latestId == 5)
            {
                DrawMenuItems(g, selectedRectangleIndex: -1, flag: false, borderColor: Color.Transparent);
            }
            else if (latestId == 6)
            {
                DrawMenuItems(g, selectedRectangleIndex: -1, flag: false, borderColor: Color.Transparent);
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
                // Draw the main product display rectangle with rounded corners
                Rectangle productRect = productRectangles[i].Item1;
                Color productRectColor = Color.FromArgb(255, 234, 233, 239);
                int cornerRadius = 50; // Circular corners

                // Draw rectangle with border if selected, else no border
                int borderFlag = (flag && i == selectedRectangleIndex) ? 1 : 0;
                Color effectiveBorderColor = (i == selectedRectangleIndex) ? borderColor : productRectColor;
                DrawRoundedRectangle(g, productRect, productRectColor, cornerRadius, effectiveBorderColor, borderFlag);

                // Draw the product image inside the rectangle
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

                // Draw the pink section at the bottom
                Rectangle bottomTextRect = productRectangles[i].Item2;
                using (SolidBrush pinkBrush = new SolidBrush(Color.FromArgb(255, 254, 81, 161)))
                {
                    g.FillRectangle(pinkBrush, bottomTextRect);
                }

                // Draw text inside the pink section
                string bottomText = "Product Details Here";
                using (Font textFont = new Font("Arial", 14, FontStyle.Bold))
                using (Brush textBrush = new SolidBrush(Color.White))
                {
                    SizeF textSize = g.MeasureString(bottomText, textFont);
                    PointF textLocation = new PointF(
                        bottomTextRect.X + (bottomTextRect.Width - textSize.Width) / 2,
                        bottomTextRect.Y + (bottomTextRect.Height - textSize.Height) / 2
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



    private void DrawSunblockInfo(Graphics g)
    {
        try
        {
            // Draw background image if available
            if (File.Exists(backgroundPath))
            {
                using (Image backgroundImg = Image.FromFile(backgroundPath))
                {
                    g.DrawImage(backgroundImg, new Rectangle(0, 0, width, height));
                }
            }

            // Draw rounded rectangle for information box
            Rectangle rect = new Rectangle(150, 165, 400, 500);
            Color rectColor = Color.FromArgb(255, 234, 233, 239);
            int cornerRadius = 20;
            DrawRoundedRectangle(g, rect, rectColor, cornerRadius, Color.FromArgb(255, 255, 178, 34), 1);

            // Define product information text
            string[] sentences = {
            "This is the first sentence that spans across three lines for layout testing purposes.",
            "Here is the second sentence, carefully crafted to ensure it also takes three lines.",
            "Finally, this is the third sentence, with enough words to stretch into three lines.",
            "This is an additional sentence to ensure the rectangle holds more text and stays balanced."
        };

            // Draw text block within the information box
            Rectangle rect2 = new Rectangle(150, 165, 400, 500);
            Font textFont = new Font("Arial", 14, FontStyle.Regular);
            Brush textBrush = new SolidBrush(Color.FromArgb(100, 100, 100));
            int verticalPadding = 95;
            int horizontalPadding = 20;
            int lineSpacing = 20;
            DrawTextBlock(g, rect2, sentences, textFont, textBrush, verticalPadding, horizontalPadding, lineSpacing);

            // Draw product image if available
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

    public void DrawTextBlock(Graphics g, Rectangle rect, string[] sentences, Font textFont, Brush textBrush, int verticalPadding, int horizontalPadding, int lineSpacing)
    {
        // Calculate initial Y position
        float startY = rect.Y + verticalPadding; // Adjusted to increase top space
        float maxWidth = rect.Width - (2 * horizontalPadding);
        float lineHeight = textFont.GetHeight(g) * 3; // Height for 3 lines of text

        // Set up the StringFormat to enable word wrapping
        StringFormat format = new StringFormat();
        format.Alignment = StringAlignment.Near;
        format.LineAlignment = StringAlignment.Near;
        format.FormatFlags = StringFormatFlags.LineLimit;

        foreach (string sentence in sentences)
        {
            // Draw the sentence in the specified rectangle area
            g.DrawString(sentence, textFont, textBrush, new RectangleF(rect.X + horizontalPadding, startY, maxWidth, lineHeight), format);
            startY += lineHeight + lineSpacing; // Move startY to the next block position
        }
    }
    private void DrawHomePage(Graphics g)
    {
        int start = (height / 3) - 30;

        // Draw the background image if it exists
        string backgroundImagePath = "home.png";
        if (File.Exists(backgroundImagePath))
        {
            using (Image bgImage = Image.FromFile(backgroundImagePath))
            {
                g.DrawImage(bgImage, new Rectangle(0, 0, width, height));
            }
        }

        // Draw the title text
        using (Font titleFont = new Font("Verdana", 24, FontStyle.Bold))
        using (SolidBrush titleBrush = new SolidBrush(Color.Purple))
        {
            g.DrawString("Pharmacy Shop", titleFont, titleBrush, new Point(50, start));
        }
        start += 80;

        // Draw the description text
        using (Font descFont = new Font("Verdana", 12))
        using (SolidBrush descBrush = new SolidBrush(Color.Gray))
        {
            g.DrawString("Experience convenient pharmacy ", descFont, descBrush, new RectangleF(50, start, 700, 50));
            start += 30;
            g.DrawString("shopping with fast delivery.", descFont, descBrush, new RectangleF(50, start, 700, 50));
            start += 30;
            g.DrawString("Get your essentials with a click!", descFont, descBrush, new RectangleF(50, start, 700, 30));
        }
        start += 80;

        // Draw 'Shop Now' button
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

        // Draw the cursor path if available

    }

    private void ShowLoginOptions()
    {
        // Clear existing controls
        this.Controls.Clear();
        this.Invalidate(); // Force redraw to ensure all previous content is cleared

        // Set the background to white
        this.BackColor = Color.White;

        // Load and draw the image on the left half of the page
        string imagePath = "login image.png"; // Replace with the actual image path
        if (File.Exists(imagePath))
        {
            PictureBox pictureBox = new PictureBox()
            {
                Image = Image.FromFile(imagePath),
                Location = new Point(0, 0), // Start at top-left corner
                Size = new Size(width / 2, height), // Cover left half of the screen
                SizeMode = PictureBoxSizeMode.StretchImage
            };
            this.Controls.Add(pictureBox);
        }


        Button userLoginButton = new Button()
        {
            Text = "Login as User",
            Location = new Point((3 * width) / 4 - 100, height / 2 - 50),
            Size = new Size(200, 50),
            BackColor = Color.FromArgb(0, 180, 216), // Button color similar to the original design
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Arial", 12, FontStyle.Bold)
        };
        userLoginButton.FlatAppearance.BorderSize = 0;
        userLoginButton.Click += (sender, e) => { MessageBox.Show("User Login Selected"); };

        // Create 'Login as Admin' button
        Button adminLoginButton = new Button()
        {
            Text = "Login as Admin",
            Location = new Point((3 * width) / 4 - 100, height / 2 + 20),
            Size = new Size(200, 50),
            BackColor = Color.FromArgb(0, 180, 216), // Button color similar to the original design
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Arial", 12, FontStyle.Bold)
        };
        adminLoginButton.FlatAppearance.BorderSize = 0;
        adminLoginButton.Click += (sender, e) => { MessageBox.Show("Admin Login Selected"); };

        // Add buttons to the form
        this.Controls.Add(userLoginButton);
        this.Controls.Add(adminLoginButton);
    }

    private void DrawLoginScreen(Graphics g)
    {
        try
        {
            // Set background to white
            g.Clear(Color.White);

            // Draw the login background image
            string loginBackgroundPath = "bk login.jpg"; // Replace with actual image path
            if (File.Exists(loginBackgroundPath))
            {
                using (Image loginBackground = Image.FromFile(loginBackgroundPath))
                {
                    g.DrawImage(loginBackground, new Rectangle(0, 0, width, height));
                }
            }

            // Draw 'Login as Customer' button
            Rectangle customerButtonRect = new Rectangle(1060, height / 2, 200, 50);
            DrawRoundedButton(g, customerButtonRect, "Login as Customer");

            string open = "Please open your bluetooth";
            Font openFont = new Font("Arial", 12, FontStyle.Italic | FontStyle.Bold);
            Brush openBrush = new SolidBrush(Color.DarkSlateBlue); // Choose a color that complements the button

            // Calculate position for the text directly under the button with a small margin
            PointF openPosition = new PointF(
                customerButtonRect.X + (customerButtonRect.Width / 2) - (g.MeasureString(open, openFont).Width / 2), // Centered under the button
                customerButtonRect.Bottom + 10 // 10 pixels below the button
            );
            g.DrawString(open, openFont, openBrush, openPosition);
            // Draw the login image on the left side of the screen
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


    private async Task StartListeningForLogin(CancellationToken token)
    {
        try
        {
            TcpListener listener = new TcpListener(System.Net.IPAddress.Parse("127.0.0.1"), 5000);
            listener.Start();

            while (!token.IsCancellationRequested)
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
                await Task.Delay(100);  // Prevent tight loop
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

    // Show message only if TUIO object 4 is present
    private void ProcessLoginMessage(string message)
    {
        if (latestIdIsFour) // Only show message if TUIO object 4 is detected
        {
            this.Invoke(new Action(() =>
            {
                string displayMessage = message == "User registered" ? "A new user has been registered." : "User has successfully logged in.";
                Color messageColor = message == "User registered" ? Color.Green : Color.Blue;

                ShowMessageOnScreen(displayMessage, messageColor);
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
}