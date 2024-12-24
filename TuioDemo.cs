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
using OfficeOpenXml;
using System.Collections.Concurrent;
using System.Linq;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using System.Data;
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
    private bool messageDisplayed = false;
    Font font = new Font("Times New Roman", 20);
    SolidBrush fntBrush = new SolidBrush(Color.White);
    SolidBrush bgrBrush = new SolidBrush(Color.FromArgb(0, 0, 64));
    SolidBrush curBrush = new SolidBrush(Color.FromArgb(192, 0, 192));
    SolidBrush objBrush = new SolidBrush(Color.FromArgb(64, 0, 0));
    SolidBrush blbBrush = new SolidBrush(Color.FromArgb(64, 64, 64));
    Pen curPen = new Pen(new SolidBrush(Color.Blue), 1);
    List<Tuple<Rectangle, Rectangle>> productRectangles = new List<Tuple<Rectangle, Rectangle>>();
    private Point lastObjectPosition = Point.Empty;
    List<string> imagePaths = new List<string> { "Sunblock.png", "dermatique1.png", "Deodrant.jpeg", "NIVEA.jpeg", "Vitamin.jpeg", "123.jpg" };
    string backgroundPath = "Untitled design (3).png";
    bool isAdmin = false;
    bool flagg = true;
    bool loggedIn = false;
    bool itemAdded = false;
    bool itemRemoved = false;
    bool customer = false;
    bool flaglogin = false;
    bool admin = false;
    bool facelogin = false;
    int OveralCart = 0;
    int OveralCart3 = 0;
    int OveralCart5 = 0;
    int OveralCart6 = 0;
    bool isright= false;
    bool isleft = false;
    bool isId17Present = false;
    private float rotationAngle = 0f; // Class-level variable for rotation
    int add45 = 0;
    int add55 = 0;
    bool isId13Presenttt = false;
    bool itemAdded55 = false;
    bool itemRemoved55 = false;
    bool savedInExcel = false;
    private string currentUserName = null;
    private int highlightedProductIndex = -1;
    private string excelFilePath = "D:\\hciii course\\hciii\\Smart_Shopping-main\\UserProfileByFace.xlsx";
    private Dictionary<string, int> userCart = new Dictionary<string, int>();

    bool flagloginbluetooth = false;
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
    Dictionary<string, (string Price, int Stock, string ImagePath)> productDetails = new Dictionary<string, (string, int, string)>
{
    { "Dermatique", ("$25", 10, "dermatique.jpg") },
    { "Sunblock", ("$15", 20, "Sunblock.png") },
    { "Deodorant", ("$10", 15, "Deodrant.jpeg") },
    { "Nivea Cream", ("$20", 5, "NIVEA.jpeg") },
    { "Vitamin D-3", ("$18", 8, "Vitamin.jpeg") },
    { "One Two Three Medicine", ("$30", 12, "medicine.jpg") }
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
    // Class for circular menu items
    class CircularMenuItem
    {
        public string Name { get; }
        public Color Color { get; }

        public CircularMenuItem(string name, Color color)
        {
            Name = name;
            Color = color;
        }
    }

    // Circular menu items with specified colors
    List<CircularMenuItem> circularMenuItems = new List<CircularMenuItem>
{
    new CircularMenuItem("About Us", ColorTranslator.FromHtml("#D3F8E2")),
    new CircularMenuItem("Our Products", ColorTranslator.FromHtml("#E4C1F9")),
    new CircularMenuItem("Home Page", ColorTranslator.FromHtml("#F694C1")),
    new CircularMenuItem("Cart", ColorTranslator.FromHtml("#EDE7B1")),
    new CircularMenuItem("Instructions", ColorTranslator.FromHtml("#A9DEF9"))
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
    private void DrawCircularMenu(Graphics g, int centerX, int centerY, int radius, int highlightedIndex = -1)
    {
        string backgroundImagePath = "backmenu.png"; // Update with your actual image path
        if (File.Exists(backgroundImagePath))
        {
            using (Image bgImage = Image.FromFile(backgroundImagePath))
            {
                g.DrawImage(bgImage, new Rectangle(0, 0, width, height));
            }
        }

        // Draw "Welcome" text and current user name at the top
        string welcomeText = "Welcome";
        string currentUserNameText = currentUserName ?? "Guest"; // Replace "Guest" if no user is logged in
        string fullWelcomeText = $"{welcomeText}, {currentUserNameText}!";

        using (Font welcomeFont = new Font("Times New Roman", 24, FontStyle.Bold))
        using (Brush welcomeBrush = new SolidBrush(Color.DarkSlateBlue))
        {
            SizeF welcomeTextSize = g.MeasureString(fullWelcomeText, welcomeFont);
            PointF welcomeTextLocation = new PointF((width - welcomeTextSize.Width) / 2, 20); // Position at the top center
            g.DrawString(fullWelcomeText, welcomeFont, Brushes.White, welcomeTextLocation.X + 2, welcomeTextLocation.Y + 2); // Shadow
            g.DrawString(fullWelcomeText, welcomeFont, welcomeBrush, welcomeTextLocation);
        }

        int itemCount = circularMenuItems.Count; // Number of items in the menu
        float angleStep = 360f / itemCount;      // Angle per menu item
        float gapAngle = 2f;                     // Gap between menu items
        Font font = new Font("Times New Roman", 18, FontStyle.Bold); // Font for labels
        Brush textBrush = new SolidBrush(ColorTranslator.FromHtml("#4a4a4a")); // Text color
        Pen linePen = new Pen(ColorTranslator.FromHtml("#4a4a4a"), 2);         // Pen for connector lines
        Pen highlightPen = new Pen(ColorTranslator.FromHtml("#FFA500"), 8);    // Highlight border (thicker width)

        string[] imagePaths = new string[]
        {
        "aboutus.png",
        "ourproducts.png",
        "home (2).png",
        "cart.png",
        "instructions.png"
        };

        for (int i = 0; i < itemCount; i++)
        {
            // Calculate start and mid angles for the current slice
            float startAngle = (i * angleStep) % 360;
            float midAngle = (startAngle + angleStep / 2) % 360;

            // If this slice is highlighted, draw a wider border and expand the selection area
            if (i == highlightedIndex)
            {
                using (Brush highlightBrush = new SolidBrush(Color.FromArgb(180, 255, 165, 0))) // Semi-transparent orange
                {
                    g.FillPie(highlightBrush, centerX - radius - 10, centerY - radius - 10, (radius + 10) * 2, (radius + 10) * 2,
                              startAngle + gapAngle / 2, angleStep - gapAngle);
                }
                g.DrawPie(highlightPen, centerX - radius - 10, centerY - radius - 10, (radius + 10) * 2, (radius + 10) * 2,
                          startAngle + gapAngle / 2, angleStep - gapAngle);
            }

            // Draw the pie slice
            using (Brush brush = new SolidBrush(circularMenuItems[i].Color))
            {
                g.FillPie(brush, centerX - radius, centerY - radius, radius * 2, radius * 2,
                          startAngle + gapAngle / 2, angleStep - gapAngle);
            }

            // Calculate the center of the slice for image placement
            double midRadians = Math.PI * midAngle / 180.0;
            int sliceCenterX = centerX + (int)(radius * 0.6 * Math.Cos(midRadians)); // Adjusted to 0.6 to move images outward
            int sliceCenterY = centerY + (int)(radius * 0.6 * Math.Sin(midRadians)); // Adjusted to 0.6 to move images outward

            // Draw image if available
            if (File.Exists(imagePaths[i]))
            {
                using (Image img = Image.FromFile(imagePaths[i]))
                {
                    // Adjust the image size for "About Us"
                    int imgSize = (i == 0) ? (int)(radius * 0.4) : (int)(radius * 0.35);
                    g.DrawImage(img, sliceCenterX - imgSize / 2, sliceCenterY - imgSize / 2, imgSize, imgSize);
                }
            }
            else
            {
                // Fallback: Draw a placeholder circle
                g.FillEllipse(Brushes.White, sliceCenterX - 20, sliceCenterY - 20, 40, 40);
            }

            // Calculate the edge of the slice (outer radius) for line connections
            int sliceEdgeX = centerX + (int)(radius * Math.Cos(midRadians));
            int sliceEdgeY = centerY + (int)(radius * Math.Sin(midRadians));

            // Calculate line endpoint (to the side) based on the position of the slice
            int lineEndX, lineEndY = sliceEdgeY;
            if (midAngle > 90 && midAngle < 270) // Left side (pink and green quarters)
            {
                lineEndX = sliceEdgeX - 100; // Extend line to the left
            }
            else // Right side (blue and yellow quarters)
            {
                lineEndX = sliceEdgeX + 100; // Extend line to the right
            }

            // Draw the connector line from the slice edge to the line endpoint
            g.DrawLine(linePen, sliceEdgeX, sliceEdgeY, lineEndX, lineEndY);

            // Draw a small circle at the endpoint of the line
            g.FillEllipse(Brushes.Black, lineEndX - 5, lineEndY - 5, 10, 10);

            // Adjust text position based on the side
            string labelText = circularMenuItems[i].Name;
            SizeF textSize = g.MeasureString(labelText, font);
            int textOffsetX = 0;

            if (midAngle > 90 && midAngle < 270) // Left side
            {
                textOffsetX = -(int)textSize.Width - 15; // Place text to the left of the dot
            }
            else // Right side
            {
                textOffsetX = 15; // Place text to the right of the dot
            }

            // Draw the label text with a shadow for better visibility
            g.DrawString(labelText, font, Brushes.White,
                         lineEndX + textOffsetX,
                         lineEndY - textSize.Height / 2 + 2); // Shadow effect
            g.DrawString(labelText, font, textBrush,
                         lineEndX + textOffsetX,
                         lineEndY - textSize.Height / 2); // Actual text
        }

        // Draw the central white circle
        g.FillEllipse(Brushes.White, centerX - 50, centerY - 50, 100, 100); // Central circle

        // Draw the central label
        string centralText = "MENU";
        Font centralFont = new Font("Times New Roman", 20, FontStyle.Bold);
        Brush centralBrush = new SolidBrush(ColorTranslator.FromHtml("#4a4a4a")); // Darker gray for central text
        SizeF centralTextSize = g.MeasureString(centralText, centralFont);
        g.DrawString(centralText, centralFont, Brushes.White,
                     centerX - centralTextSize.Width / 2 + 2, centerY - centralTextSize.Height / 2 + 2); // Shadow
        g.DrawString(centralText, centralFont, centralBrush,
                     centerX - centralTextSize.Width / 2, centerY - centralTextSize.Height / 2); // Actual text
    }

    private void DrawAboutUsPage(Graphics g)
    {
        try
        {
            // Background
            string backgroundImagePath = "about_us_bg.jpg"; // Update with your actual image path
            if (File.Exists(backgroundImagePath))
            {
                using (Image bgImage = Image.FromFile(backgroundImagePath))
                {
                    g.DrawImage(bgImage, new Rectangle(0, 0, width, height));
                }
            }

            // Semi-transparent overlay for title and description
            Rectangle overlayRect = new Rectangle(50, 50, width - 100, 200);
            DrawRoundedRectangleWithShadow(g, overlayRect, Color.FromArgb(180, 220, 220, 220), 20, Color.Gray);

            // Title - About Us
            string title = "About Us";
            using (Font titleFont = new Font("Verdana", 32, FontStyle.Bold))
            using (Brush titleBrush = new SolidBrush(Color.Black)) // Dark Gray color for the title
            {
                g.DrawString(title, titleFont, titleBrush, new PointF(width / 2 - 100, 70));
            }

            // Description
            string description = "Welcome to the Smart Pharmacy Shop!\n\n" +
                "Our mission is to provide an innovative shopping experience using advanced technologies. " +
                "With a combination of gesture recognition, object detection, and face recognition, " +
                "we aim to make shopping seamless, informative, and enjoyable.\n\n" +
                "Thank you for choosing us!";
            using (Font descFont = new Font("Times New Roman", 14))
            using (Brush descBrush = new SolidBrush(Color.Black))
            {
                g.DrawString(description, descFont, descBrush, new RectangleF(overlayRect.X + 20, overlayRect.Y + 50, overlayRect.Width - 40, overlayRect.Height - 60));
            }

            // Team Section Header - Meet Our Team
            string teamHeader = "Meet Our Team:";
            using (Font teamFont = new Font("Times New Roman", 20, FontStyle.Bold))
            using (Brush teamBrush = new SolidBrush(Color.Black)) // Dark Gray color for the team header
            {
                g.DrawString(teamHeader, teamFont, teamBrush, new PointF(50, 280));
            }

            // Team Member Cards
            string[] teamMembers = { "Tarek", "Farah", "Youssef", "Malak", "Roqaia", "Rawan" };
            string[] descriptions = {
         "Tarek - Software Engineer specializing in backend development.",
         "Farah - UI/UX Designer creating intuitive user interfaces.",
         "Youssef - Frontend Developer with a passion for web technologies.",
         "Malak - Project Manager ensuring smooth delivery of projects.",
         "Roqaia - Data Scientist analyzing trends and building models.",
         "Rawan - QA Engineer dedicated to software quality."
     };
            bool[] isMale = { true, false, true, false, false, false }; // Gender of team members
            string maleIconPath = "male.png";
            string femaleIconPath = "female.png";

            int cardWidth = 300;
            int cardHeight = 200;
            int startX = 250;
            int startY = 320;
            int spacingX = 50;
            int spacingY = 30;

            for (int i = 0; i < teamMembers.Length; i++)
            {
                int row = i / 3;
                int col = i % 3;

                int x = startX + col * (cardWidth + spacingX);
                int y = startY + row * (cardHeight + spacingY);

                Rectangle cardRect = new Rectangle(x, y, cardWidth, cardHeight);
                DrawRoundedRectangleWithShadow(g, cardRect, Color.FromArgb(180, 169, 169, 169), 15, Color.Gray); // Reduced opacity and gray color

                // Add icon
                string iconPath = isMale[i] ? maleIconPath : femaleIconPath;
                if (File.Exists(iconPath))
                {
                    using (Image icon = Image.FromFile(iconPath))
                    {
                        g.DrawImage(icon, new Rectangle(x + 10, y + 20, 40, 40));
                    }
                }

                // Add name
                using (Font nameFont = new Font("Times New Roman", 14, FontStyle.Bold))
                using (Brush nameBrush = new SolidBrush(Color.Black))
                {
                    g.DrawString(teamMembers[i], nameFont, nameBrush, new PointF(x + 60, y + 20));
                }

                // Add description
                using (Font descFont = new Font("Times New Roman", 12, FontStyle.Regular))
                using (Brush descBrush = new SolidBrush(Color.Black))
                {
                    g.DrawString(descriptions[i], descFont, descBrush, new RectangleF(x + 10, y + 60, cardWidth - 20, cardHeight - 70));
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error drawing About Us page: {ex.Message}");
        }
    }


    private void DrawRoundedRectangleWithShadow(Graphics g, Rectangle rect, Color fillColor, int cornerRadius, Color shadowColor)
    {
        int shadowOffset = 5;
        using (GraphicsPath path = new GraphicsPath())
        {
            int diameter = cornerRadius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            // Draw shadow
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(100, shadowColor)))
            {
                g.TranslateTransform(shadowOffset, shadowOffset);
                g.FillPath(shadowBrush, path);
                g.TranslateTransform(-shadowOffset, -shadowOffset);
            }

            // Fill rectangle
            using (SolidBrush fillBrush = new SolidBrush(fillColor))
            {
                g.FillPath(fillBrush, path);
            }
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
    private void DrawProductDetails(Graphics g, TuioObject tobj, PointF location, Color textColor)
    {
        // Updated product details for Degree Cool Rush Deodorant
        List<string> productDetails = new List<string>
    {
        "Brand: Degree",
        "Type: Cool Rush Deodorant",
        "Features: 48-hour protection, invigorating Cool Rush scent",
        "Price: $4.99",
        "Usage: Apply evenly to underarms for all-day freshness"
    };

        // Set the starting position for drawing the details
        float yOffset = 0;

        // Use the provided color for the text
        using (Brush textBrush = new SolidBrush(textColor))
        {
            foreach (var detail in productDetails)
            {
                g.DrawString($" {detail}", font, textBrush, new PointF(location.X, location.Y + yOffset));
                yOffset += font.GetHeight(g) + 2; // Adjust vertical spacing between lines
            }
        }
    }

    private void DrawProductDetails2(Graphics g, TuioObject tobj, PointF location, Color textColor)
    {
        // Updated product details for Degree Cool Rush Deodorant
        List<string> productDetails = new List<string>
    {
        "Brand: NOW",
        "Type: High Potency Vitamin D-3",
        "Features: 1,000 IU per softgel, supports bone and immune health",
        "Price: $8",
        "Usage: Take one softgel daily with a meal, or as directed by a healthcare professional"
    };

        // Set the starting position for drawing the details
        float yOffset = 0;

        // Use the provided color for the text
        using (Brush textBrush = new SolidBrush(textColor))
        {
            foreach (var detail in productDetails)
            {
                g.DrawString($" {detail}", font, textBrush, new PointF(location.X, location.Y + yOffset));
                yOffset += font.GetHeight(g) + 2; // Adjust vertical spacing between lines
            }
        }
    }
    private void DrawMonitorExpressionsPage(Graphics g)
    {
        try
        {
            // Background
            string backgroundPath = "about_us_bgg.jpg"; // Update with your actual background image path
            if (File.Exists(backgroundPath))
            {
                using (Image background = Image.FromFile(backgroundPath))
                {
                    g.DrawImage(background, new Rectangle(0, 0, width, height));
                }
            }

            // Title
            string title = "Monitor Expressions";
            using (Font titleFont = new Font("Times New Roman", 32, FontStyle.Bold))
            using (Brush titleBrush = new SolidBrush(ColorTranslator.FromHtml("#4a4a4a"))) // Dark gray
            {
                SizeF titleSize = g.MeasureString(title, titleFont);
                PointF titleLocation = new PointF((width - titleSize.Width) / 2, 50);

                // Add shadow for the title
                g.DrawString(title, titleFont, Brushes.White, new PointF(titleLocation.X + 2, titleLocation.Y + 2));
                g.DrawString(title, titleFont, titleBrush, titleLocation);
            }

            // Read and process Excel data
            string excelFilePath = "D:\\hciii course\\hciii\\Smart_Shopping-main\\data.xlsx"; // Update with your actual file path
            if (File.Exists(excelFilePath))
            {
                DataTable dataTable = LoadExcelAsDataTable(excelFilePath);

                // Calculate emotion times for each user
                var emotionTimeSummary = CalculateEmotionTimes(dataTable);

                // Emotion icons dictionary
                var emotionIcons = new Dictionary<string, string>
            {
                { "happy", "happy.png" },
                { "sad", "sad.png" },
                { "angry", "angry.png" },
                { "neutral", "neatral.png" },
                { "surprise", "surprice.png" }
            };

                // Display emotion rates
                int startX = 100;
                int startY = 150;
                int rowHeight = 60;
                int columnWidth = 250;
                int iconSize = 40;

                using (Font headerFont = new Font("Times New Roman", 16, FontStyle.Bold))
                using (Font cellFont = new Font("Times New Roman", 14, FontStyle.Regular))
                using (Brush headerBrush = new SolidBrush(Color.White))
                using (Brush cellBrush = new SolidBrush(Color.Black))
                {
                    // Draw headers
                    g.DrawString("User Name", headerFont, headerBrush, new PointF(startX, startY));
                    g.DrawString("Emotion", headerFont, headerBrush, new PointF(startX + columnWidth, startY));
                    g.DrawString("Total Time (s)", headerFont, headerBrush, new PointF(startX + columnWidth * 2, startY));

                    startY += rowHeight;

                    // Draw data
                    foreach (var userEmotion in emotionTimeSummary)
                    {
                        string userName = userEmotion.Key;
                        foreach (var emotionData in userEmotion.Value)
                        {
                            string emotion = emotionData.Key;
                            string totalTime = emotionData.Value.ToString();

                            // Draw user name and total time
                            g.DrawString(userName, cellFont, cellBrush, new PointF(startX, startY));
                            g.DrawString(emotion, cellFont, cellBrush, new PointF(startX + columnWidth, startY));
                            g.DrawString(totalTime+"   seconds", cellFont, cellBrush, new PointF(startX + columnWidth * 2, startY));

                            // Draw emotion icon
                            if (emotionIcons.ContainsKey(emotion.ToLower()))
                            {
                                string iconPath = emotionIcons[emotion.ToLower()];
                                if (File.Exists(iconPath))
                                {
                                    using (Image emotionIcon = Image.FromFile(iconPath))
                                    {
                                        g.DrawImage(emotionIcon, new Rectangle(startX + columnWidth - 50, startY, iconSize, iconSize));
                                    }
                                }
                            }

                            startY += rowHeight;
                        }
                    }
                }
            }
            else
            {
                string error = "Excel file not found.";
                using (Font errorFont = new Font("Times New Roman", 16, FontStyle.Italic))
                using (Brush errorBrush = new SolidBrush(Color.Red))
                {
                    g.DrawString(error, errorFont, errorBrush, new PointF(100, 150));
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error drawing Monitor Expressions page: {ex.Message}");
        }
    }


    private Dictionary<string, Dictionary<string, double>> CalculateEmotionTimes(DataTable dataTable)
    {
        var emotionTimeSummary = new Dictionary<string, Dictionary<string, double>>();

        foreach (DataRow row in dataTable.Rows)
        {
            string userName = row[0]?.ToString() ?? "Unknown";
            string emotion = row[1]?.ToString() ?? "Unknown";

            // Parse the time value
            if (!DateTime.TryParse(row[2]?.ToString(), out DateTime timestamp))
            {
                continue;
            }

            // Initialize user and emotion if not already present
            if (!emotionTimeSummary.ContainsKey(userName))
            {
                emotionTimeSummary[userName] = new Dictionary<string, double>();
            }

            if (!emotionTimeSummary[userName].ContainsKey(emotion))
            {
                emotionTimeSummary[userName][emotion] = 0;
            }

            // Add the duration (assuming rows are sorted by time for simplicity)
            emotionTimeSummary[userName][emotion] += timestamp.TimeOfDay.TotalSeconds;
        }

        return emotionTimeSummary;
    }



    private DataTable LoadExcelAsDataTable(string filePath)
    {
        DataTable dataTable = new DataTable();

        try
        {
            using (var package = new OfficeOpenXml.ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets["Sheet1"];
                foreach (var firstRowCell in worksheet.Cells[1, 1, 1, worksheet.Dimension.End.Column])
                {
                    dataTable.Columns.Add(firstRowCell.Text);
                }

                for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                {
                    var newRow = dataTable.NewRow();
                    for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                    {
                        newRow[col - 1] = worksheet.Cells[row, col].Text;
                    }
                    dataTable.Rows.Add(newRow);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error reading Excel file: {ex.Message}");
        }

        return dataTable;
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        bool isId12Present = false;
        bool isId3Present = false;
      
        int highlightedIndex = -1;


        drawmenu();
        Graphics g = pevent.Graphics;
        if (customer == true)
        {
            DrawMenuItems(g, selectedRectangleIndex: 0, flag: true, borderColor: Color.Orange);
        }

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
                   
                }
                foreach (TuioObject tobj in objectList.Values)
                {
                    if (tobj.SymbolID == 5) isId3Present = true;
                   
                }
                foreach (TuioObject tobj in objectList.Values)
                {
                    if (tobj.SymbolID == 6) isId3Present = true;
                   
                }
            

                foreach (TuioObject tobj in objectList.Values)
                {
                    if (tobj.SymbolID == 3 || tobj.SymbolID == 5 || tobj.SymbolID == 6)
                    {
                        if (tobj.SymbolID == 3)
                        {
                            // Update OverallCart based on the angle
                            if (tobj.AngleDegrees > 30 && tobj.AngleDegrees < 90 && !itemAdded)
                            {
                                OveralCart++;
                                OveralCart3++;
                                itemAdded = true;
                                itemRemoved = false;
                            }
                            else if (tobj.AngleDegrees > 270 && tobj.AngleDegrees < 310 && !itemRemoved && OveralCart > 0)
                            {
                                OveralCart--;
                                OveralCart3--;
                                itemRemoved = true;
                                itemAdded = false;
                            }
                            else if (tobj.AngleDegrees <= 30 || tobj.AngleDegrees >= 310)
                            {
                                itemAdded = false;
                                itemRemoved = false;
                            }
                        }
                        else if (tobj.SymbolID == 5)
                        {
                            // Update OverallCart based on the angle
                            if (tobj.AngleDegrees > 30 && tobj.AngleDegrees < 90 && !itemAdded)
                            {
                                OveralCart++;
                                OveralCart5++;
                                itemAdded = true;
                                itemRemoved = false;
                            }
                            else if (tobj.AngleDegrees > 270 && tobj.AngleDegrees < 310 && !itemRemoved && OveralCart > 0)
                            {
                                OveralCart--;
                                OveralCart5--;
                                itemRemoved = true;
                                itemAdded = false;
                            }
                            else if (tobj.AngleDegrees <= 30 || tobj.AngleDegrees >= 310)
                            {
                                itemAdded = false;
                                itemRemoved = false;
                            }
                        }
                        else if (tobj.SymbolID == 6)
                        {
                            // Update OverallCart based on the angle
                            if (tobj.AngleDegrees > 30 && tobj.AngleDegrees < 90 && !itemAdded)
                            {
                                OveralCart++;
                                OveralCart6++;
                                itemAdded = true;
                                itemRemoved = false;
                            }
                            else if (tobj.AngleDegrees > 270 && tobj.AngleDegrees < 310 && !itemRemoved && OveralCart > 0)
                            {
                                OveralCart--;
                                OveralCart6--;
                                itemRemoved = true;
                                itemAdded = false;
                            }
                            else if (tobj.AngleDegrees <= 30 || tobj.AngleDegrees >= 310)
                            {
                                itemAdded = false;
                                itemRemoved = false;
                            }
                        }



                    }

                }
                foreach (TuioObject tobj in objectList.Values)
                {
                    int ox = tobj.getScreenX(width);
                    int oy = tobj.getScreenY(height);
                    int size = height / 10;
                    latestId = tobj.SymbolID;
                    if (tobj.SymbolID == 45)
                    {
                        
                        if (tobj.AngleDegrees > 30 && tobj.AngleDegrees < 90 && !itemAdded)
                        {
                            add45++;

                            itemAdded = true;
                            itemRemoved = false;

                        }
                        else if (tobj.AngleDegrees > 270 && tobj.AngleDegrees < 310 && !itemRemoved && add45 > 0)
                        {
                            add45--;

                            itemRemoved = true;
                            itemAdded = false;
                        }
                        else if (tobj.AngleDegrees <= 30 || tobj.AngleDegrees >= 310)
                        {
                            itemAdded = false;
                            itemRemoved = false;
                        }
                        DrawDeodorantInfo(g);
                    }
                    else if (tobj.SymbolID == 55)
                    {
                        //DrawVitaminInfo(Graphics.FromHwnd(this.Handle));
                        if (tobj.AngleDegrees > 30 && tobj.AngleDegrees < 90 && !itemAdded)
                        {
                            add55++;

                            itemAdded55 = true;
                            itemRemoved55 = false;

                        }
                        else if (tobj.AngleDegrees > 270 && tobj.AngleDegrees < 310 && !itemRemoved && add55 > 0)
                        {
                            add55--;

                            itemRemoved55 = true;
                            itemAdded55 = false;
                        }
                        else if (tobj.AngleDegrees <= 30 || tobj.AngleDegrees >= 310)
                        {
                            itemAdded55 = false;
                            itemRemoved55 = false;
                        }
                        DrawVitaminInfo(g);
                    }
                    else if (tobj.SymbolID == 3 && flagg)
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
                    else if (tobj.SymbolID == 5  && flagg)
                    {
                        flaglogin = false;

                        DrawDermatiqueinfo(g);
                    }
                    else if (tobj.SymbolID == 11)
                    {
                        DrawAboutUsPage(g);
                    }
                    else if (tobj.SymbolID == 10)
                    {
                        flaglogin = false;
                        int centerX = width / 2;
                        int centerY = height / 2;
                        int radius = 300;         // Radius of the circular menu
                        Image backgroundImage = Image.FromFile("backmenu.png");  // Replace with the actual image path

                        // Draw the background image to cover the full screen
                        g.DrawImage(backgroundImage, 0, 0, width, height);

                        // Call the DrawCircularMenu function
                        DrawCircularMenu(g, centerX, centerY, radius, highlightedIndex);

                    }
                    else if (tobj.SymbolID == 5 && flagg)
                    {
                        flaglogin = false;

                        DrawMenuItems(g, selectedRectangleIndex: 1, flag: true, borderColor: Color.Green);
                    }else if (tobj.SymbolID == 20)
                    {
                        DrawMonitorExpressionsPage(g);
                    }
                    else if (tobj.SymbolID == 6  && flagg)
                    {
                        flaglogin = false;

                        DrawSunblockInfo(g);
                    }
                    else if (tobj.SymbolID == 6 && flagg)
                    {
                        flaglogin = false;

                        DrawMenuItems(g, selectedRectangleIndex: 2, flag: true, borderColor: Color.Green);
                    }
                    else if (tobj.SymbolID == 55)
                    {
                        DrawVitaminInfo(g);
                    }
                    else if (tobj.SymbolID == 13)
                    {
                        DrawCrudPage(g);
                    }
                    else if(tobj.SymbolID == 21)
                    {
                        DrawProductsPage(g);
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
            lock (objectList)
            {
                foreach (TuioObject tobj in objectList.Values)
                {
                    if (tobj.SymbolID == 13) // Detect TUI with SymbolID 13
                    {
                        isId13Presenttt = true;

                        // Calculate the highlighted product index based on rotation
                        rotationAngle = (float)tobj.AngleDegrees;
                        int productCount = 4; // Number of products (adjust if more products are added)

                        highlightedProductIndex = (int)((rotationAngle % 360) / (360f / productCount));
                        highlightedProductIndex = (highlightedProductIndex + productCount) % productCount; // Ensure valid index
                    }
                }

                if (isId13Presenttt)
                {
                    // Redraw the CRUD page with the highlighted product
                    Invalidate(); // This triggers a redraw by calling OnPaint
                }
            }


            lock (objectList)
            {
                foreach (TuioObject tobj in objectList.Values)
                {
                    if (tobj.SymbolID == 12) // Detect if the object with SymbolID 12 is present
                    {
                        isId12Present = true;

                        rotationAngle = (float)tobj.AngleDegrees; // Update rotation
                        highlightedIndex = (int)((rotationAngle % 360) / (360f / circularMenuItems.Count));
                        highlightedIndex = (highlightedIndex + circularMenuItems.Count) % circularMenuItems.Count; // Valid index
                    }
                    if (tobj.SymbolID == 17) // Detect if the object with SymbolID 17 is present
                    {
                        isId17Present = true;

                    }
                }
                if (isId12Present)
                {
                    int centerX = width / 2;
                    int centerY = height / 2;
                    int radius = 300;         // Radius of the circular menu
                    Image backgroundImage = Image.FromFile("backmenu.png");  // Replace with the actual image path

                    // Draw the background image to cover the full screen
                    g.DrawImage(backgroundImage, 0, 0, width, height);

                    DrawCircularMenu(g, centerX, centerY, radius, highlightedIndex);
                    if (isId17Present && highlightedIndex != -1)
                    {
                        // Take action based on the highlighted slice
                        string selectedSliceName = circularMenuItems[highlightedIndex].Name;

                        // Open a new page or perform an action based on the slice name
                        switch (selectedSliceName)
                        {
                            case "About Us":
                                //aboutus = true;
                                DrawAboutUsPage(g);
                                isId17Present = false;
                                latestId = 11; // Update latestId when SymbolID 17 is detected
                                break;
                            case "Our Products":

                                latestId = 2;
                                DrawMenuItems(g, selectedRectangleIndex: -1, flag: false, borderColor: Color.Transparent);
                                isId17Present = false;

                                break;
                            case "Home Page":
                                DrawHomePage(g);
                                latestId = 0;
                                isId17Present = false;

                                break;
                            case "Cart":
                                DrawCrudPage(g);
                                latestId = 13;
                                isId17Present = false;
                                break;
                            case "Instructions":
                                //OpenInstructionsPage();
                                break;
                            default:
                                // Default action if needed
                                break;
                        }
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
            else if (latestId == 3 && flagg)
            {
                DrawSunblockInfo(g);
            }
            else if (latestId == 3 && flagg)
            {
                DrawMenuItems(g, selectedRectangleIndex: -1, flag: false, borderColor: Color.Transparent);
            }
            else if (latestId == 2 && flagg)
            {
                DrawMenuItems(g, selectedRectangleIndex: -1, flag: false, borderColor: Color.Transparent);
            }
            else if (latestId == 5 && flagg)
            {
                DrawDermatiqueinfo(g);
            }
            else if (latestId == 6 && flagg)
            {
                DrawMenuItems(g, selectedRectangleIndex: -1, flag: false, borderColor: Color.Transparent);
            }
            else if (latestId == 1 && flagg || admin == true)
            {
                ShowAdminMenu(g);
            }
            else if (latestId == 11)
            {
                DrawAboutUsPage(g);
            }else if (latestId == 21)
            {
                DrawProductsPage(g);
            }
           
            else if (latestId == 12)
            {
                int centerX = width / 2;
                int centerY = height / 2;
                int radius = 300;         // Radius of the circular menu
                Image backgroundImage = Image.FromFile("backmenu.png");  // Replace with the actual image path

                // Draw the background image to cover the full screen
                g.DrawImage(backgroundImage, 0, 0, width, height);

                DrawCircularMenu(g, centerX, centerY, radius, highlightedIndex);
            }
            else if (latestId == 13)
            {
                DrawCrudPage(g);
            }
            else if (latestId == 10)
            {
                int centerX = width / 2;
                int centerY = height / 2;
                int radius = 300;         // Radius of the circular menu
                Image backgroundImage = Image.FromFile("backmenu.png");  // Replace with the actual image path

                // Draw the background image to cover the full screen
                g.DrawImage(backgroundImage, 0, 0, width, height);

                // Call the DrawCircularMenu function
                DrawCircularMenu(g, centerX, centerY, radius);
            }
            else if (latestId == 20)
            {
                DrawMonitorExpressionsPage(g);
            }
            else if(latestId==45)
            {
                DrawDeodorantInfo(g);

            }
            else if (latestId == 55)
            {
                DrawVitaminInfo(g);
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
        if (latestIdIsOne || admin == true)
        {
            ShowAdminMenu(g);
        }
        if (customer == true)
        {
            DrawMenuItems(g, selectedRectangleIndex: -1, flag: false, borderColor: Color.Transparent);
        }
    }
    private void SaveUserCartToExcel(string userName, bool showMessage = true)
    {
        try
        {
            savedInExcel = true;

            // Map quantities to product categories
            var userCartQuantities = new Dictionary<string, int>
        {
            { "quantity", OveralCart },         // Total quantity of items
            { "quantityOfSunblock", OveralCart3 }, // Quantity of Sunblock
            { "quantityOfWash", OveralCart5 }     // Quantity of Wash
        };

            using (var package = new ExcelPackage(new FileInfo(excelFilePath)))
            {
                // Get or create the worksheet
                var worksheet = package.Workbook.Worksheets["Sheet1"] ?? package.Workbook.Worksheets.Add("Sheet1");

                // Ensure the worksheet has headers
                if (worksheet.Dimension == null)
                {
                    worksheet.Cells[1, 1].Value = "Name";
                    worksheet.Cells[1, 2].Value = "quantity";
                    worksheet.Cells[1, 3].Value = "products";
                    worksheet.Cells[1, 4].Value = "emotion";
                    worksheet.Cells[1, 5].Value = "quantityOfSunblock";
                    worksheet.Cells[1, 6].Value = "quantityOfWash";
                }

                int totalRows = worksheet.Dimension?.Rows ?? 1;
                bool userExists = false;

                // Iterate through rows to find the current user
                for (int row = 2; row <= totalRows; row++)
                {
                    string nameInRow = worksheet.Cells[row, 1].Text;

                    if (nameInRow.Equals(userName, StringComparison.OrdinalIgnoreCase))
                    {
                        userExists = true;

                        // Update the user's cart data
                        worksheet.Cells[row, 2].Value = userCartQuantities["quantity"]; // Total quantity
                        worksheet.Cells[row, 5].Value = userCartQuantities["quantityOfSunblock"]; // Sunblock quantity
                        worksheet.Cells[row, 6].Value = userCartQuantities["quantityOfWash"]; // Wash quantity

                        // Concatenate products based on quantities
                        var products = new List<string>();
                        if (OveralCart3 > 0) products.Add("Sunblock");
                        if (OveralCart5 > 0) products.Add("Wash");
                        worksheet.Cells[row, 3].Value = string.Join(", ", products);

                        package.Save();
                        if (showMessage) MessageBox.Show("User cart successfully updated in Excel.");
                        return;
                    }
                }

                // If the user does not exist, optionally handle this case
                if (!userExists)
                {
                    if (showMessage) MessageBox.Show($"User {userName} does not exist in the sheet.");
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving user cart to Excel: {ex.Message}");
        }
    }

    private void DrawSunblockInfo(Graphics g)
    {
        try
        {
            // Load background image
            using (Image bgImage = Image.FromFile("plain_bk.png"))
            {
                g.DrawImage(bgImage, new Rectangle(0, 0, width, height));
            }

            // Draw rounded rectangle for text content
            Rectangle rect = new Rectangle(150, 165, 400, 500);
            Color rectColor = Color.FromArgb(255, 234, 233, 239);
            int cornerRadius = 20;
            DrawRoundedRectangle(g, rect, rectColor, cornerRadius, Color.FromArgb(255, 255, 178, 34), 1);

            // Product details text
            string[] sentences = {
            "Dermatique Sunblock: Broad-spectrum SPF protection.",
            "Lightweight, non-greasy, and quick-absorbing formula.",
            "Hydrates skin while shielding from UV damage.",
            "Ideal for daily use on all skin types."
        };

            Rectangle rect2 = new Rectangle(150, 165, 400, 500);
            Font textFont = new Font("Times New Roman", 14, FontStyle.Regular);
            Brush textBrush = new SolidBrush(Color.FromArgb(100, 100, 100));
            int verticalPadding = 95;
            int horizontalPadding = 20;
            int lineSpacing = 20;
            DrawTextBlock(g, rect2, sentences, textFont, textBrush, verticalPadding, horizontalPadding, lineSpacing);

            // Display product image
            if (File.Exists(imagePaths[0]))
            {
                using (Image img = Image.FromFile(imagePaths[0]))
                {
                    g.DrawImage(img, new Rectangle(width - 600, 50, 400, 600));
                }
            }

            // Cart icon
            string cartIconPath = "cartt.png"; // Path to the cart icon image
            if (File.Exists(cartIconPath))
            {
                using (Image cartIcon = Image.FromFile(cartIconPath))
                {
                    g.DrawImage(cartIcon, new Rectangle(20, 20, 30, 30)); // Position the cart icon
                }
            }

            // Display overall cart count
            string cartText = $"Overall Cart: {OveralCart3}";
            using (Font cartFont = new Font("Times New Roman", 16, FontStyle.Bold))
            using (SolidBrush cartBrush = new SolidBrush(Color.Black))
            {
                g.DrawString(cartText, cartFont, cartBrush, new PointF(60, 20)); // Adjust the position as needed
            }

            // Display product price
            string priceText = "Price: $15";
            using (Font priceFont = new Font("Times New Roman", 16, FontStyle.Bold))
            using (SolidBrush priceBrush = new SolidBrush(Color.Black))
            {
                g.DrawString(priceText, priceFont, priceBrush, new PointF(20, 60));
            }

            // Calculate and display total price
            int totalPrice = OveralCart3 * 15; // Assuming price is $15 per item
            string totalPriceText = $"Total: ${totalPrice}";
            using (Font totalFont = new Font("Times New Roman", 16, FontStyle.Bold))
            using (SolidBrush totalBrush = new SolidBrush(Color.Black))
            {
                g.DrawString(totalPriceText, totalFont, totalBrush, new PointF(20, 100));
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
            // Get canvas dimensions
            int canvasWidth = (int)g.VisibleClipBounds.Width;
            int canvasHeight = (int)g.VisibleClipBounds.Height;

            // Number of items per row and total rows needed
            int itemsPerRow = 3; // 3 items per row
            int totalRows = (int)Math.Ceiling(productRectangles.Count / (float)itemsPerRow);

            // Adjust padding and calculate smaller dimensions for the rectangles
            int padding = 80; // Space between items
            int rectWidth = (canvasWidth - (padding * (itemsPerRow + 1))) / itemsPerRow - 80; // Reduced width
            int rectHeight = (int)(rectWidth * 1.0); // Aspect ratio of 1.0 (square-like)

            // Total grid height calculation
            int totalGridHeight = totalRows * (rectHeight + padding) - padding;

            // Center the grid vertically and horizontally
            int totalGridWidth = itemsPerRow * (rectWidth + padding) - padding;
            int offsetX = (canvasWidth - totalGridWidth) / 2;
            int offsetY = (canvasHeight - totalGridHeight) / 2;

            // Path to the background image
            string backgroundPath = "plain_bk.png";

            // Draw the background
            if (File.Exists(backgroundPath))
            {
                using (Image backgroundImg = Image.FromFile(backgroundPath))
                {
                    g.DrawImage(backgroundImg, new Rectangle(0, 0, canvasWidth, canvasHeight));
                }
            }

            // Dictionary to store product prices
            Dictionary<string, string> productPrices = new Dictionary<string, string>
           {
            { "Dermatique", "$25" },
            { "Sunblock", "$15" },
            { "Deodorant", "$10" },
            { "Nivea Cream", "$20" },
            { "Vitamin D-3", "$18" },
            { "One Two Three Medicine", "$30" }
           };

            // Loop through each product rectangle
            for (int i = 0; i < productRectangles.Count; i++)
            {
                // Calculate row and column for the current item
                int row = i / itemsPerRow;
                int col = i % itemsPerRow;

                // Calculate position with padding and offsets for centering
                int x = offsetX + col * (rectWidth + padding);
                int y = offsetY + row * (rectHeight + padding);

                // Define product rectangle
                Rectangle productRect = new Rectangle(x, y, rectWidth, rectHeight);
                Color productRectColor = Color.FromArgb(255, 234, 233, 239);

                // Draw rectangle border
                bool drawBorder = (flag && i == selectedRectangleIndex);
                Color effectiveBorderColor = drawBorder ? borderColor : productRectColor;
                int borderWidth = drawBorder ? 3 : 0;

                DrawRectangleBorder(g, productRect, productRectColor, effectiveBorderColor, borderWidth, drawBorder);

                // Draw the product image
                if (File.Exists(imagePaths[i]))
                {
                    using (Bitmap productImg = new Bitmap(imagePaths[i]))
                    {
                        Color transparentColor = productImg.GetPixel(0, 0);
                        productImg.MakeTransparent(transparentColor);

                        // Scale image proportionally to fit inside the rectangle
                        int imgWidth = (int)(rectWidth * 0.6f); // Reduced size for better fit
                        int imgHeight = (int)(rectHeight * 0.6f);

                        // Center the image within the rectangle
                        int imgX = x + (rectWidth - imgWidth) / 2;
                        int imgY = y + (rectHeight - imgHeight - (int)(rectHeight * 0.2f)) / 2;

                        g.DrawImage(productImg, new Rectangle(imgX, imgY, imgWidth, imgHeight));
                    }
                }

                // Define and draw the pink text section BELOW the product rectangle
                int textSectionHeight = (int)(rectHeight * 0.15f); // Reduced height of text section
                Rectangle bottomTextRect = new Rectangle(
                    x,
                    y + rectHeight - 10, // Position it just below the product rectangle
                    rectWidth,
                    textSectionHeight
                );

                using (SolidBrush pinkBrush = new SolidBrush(Color.BurlyWood))
                {
                    g.FillRectangle(pinkBrush, bottomTextRect);
                }

                // Draw text inside the pink section
                string bottomText;
                string productName;
                switch (i)
                {
                    case 0: productName = "Sunblock"; break;
                    case 1: productName = "Dermatique"; break;
                    case 2: productName = "Deodorant"; break;
                    case 3: productName = "Nivea Cream"; break;
                    case 4: productName = "Vitamin D-3"; break;
                    case 5: productName = "One Two Three Medicine"; break;
                    default: productName = "Unknown"; break;
                }

                string price = productPrices.ContainsKey(productName) ? productPrices[productName] : "N/A";
                bottomText = $"{productName}\nPrice: {price}";

                using (Font textFont = new Font("Times New Roman", 12, FontStyle.Bold)) // Adjust font size if needed
                using (Brush textBrush = new SolidBrush(Color.White))
                {
                    SizeF textSize = g.MeasureString(bottomText, textFont);
                    PointF textLocation = new PointF(
                        bottomTextRect.X + (bottomTextRect.Width - textSize.Width) / 2.0f,
                        bottomTextRect.Y + (bottomTextRect.Height - textSize.Height) / 2.0f
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
    private void DrawProductsPage(Graphics g)
    {
        try
        {
            // Background
            string backgroundPath = "about_us_bgg.jpg"; // Update with your actual background image path
            if (File.Exists(backgroundPath))
            {
                using (Image background = Image.FromFile(backgroundPath))
                {
                    g.DrawImage(background, new Rectangle(0, 0, width, height));
                }
            }

            // Title
            string title = "Products Management";
            using (Font titleFont = new Font("Times New Roman", 32, FontStyle.Bold))
            using (Brush titleBrush = new SolidBrush(ColorTranslator.FromHtml("#4a4a4a")))
            {
                SizeF titleSize = g.MeasureString(title, titleFont);
                PointF titleLocation = new PointF((width - titleSize.Width) / 2, 50);

                // Add shadow for the title
                g.DrawString(title, titleFont, Brushes.White, new PointF(titleLocation.X + 2, titleLocation.Y + 2));
                g.DrawString(title, titleFont, titleBrush, titleLocation);
            }

            // Product list rendering
            int startX = 100;
            int startY = 150;
            int rowHeight = 150; // Increased row height to fit images
            int columnWidth = 300;
            int imageSize = 100; // Image size for each product

            using (Font cellFont = new Font("Times New Roman", 14, FontStyle.Regular))
            using (Brush cellBrush = new SolidBrush(Color.Black))
            using (Brush buttonBrush = new SolidBrush(Color.Red))
            {
                foreach (var product in productDetails)
                {
                    string productName = product.Key;
                    string price = product.Value.Price;
                    int stock = product.Value.Stock;
                    string imagePath = product.Value.ImagePath;

                    // Draw product image
                    if (File.Exists(imagePath))
                    {
                        using (Image productImage = Image.FromFile(imagePath))
                        {
                            g.DrawImage(productImage, new Rectangle(startX, startY, imageSize, imageSize));
                        }
                    }

                    // Draw product name, price, and stock
                    g.DrawString($"Product: {productName}", cellFont, cellBrush, new PointF(startX + imageSize + 20, startY));
                    g.DrawString($"Price: {price}", cellFont, cellBrush, new PointF(startX + imageSize + 20, startY + 30));
                    g.DrawString($"Stock: {stock}", cellFont, cellBrush, new PointF(startX + imageSize + 20, startY + 60));

                    // Draw "Delete" button
                    Rectangle deleteButton = new Rectangle(startX + columnWidth * 2, startY + (rowHeight / 2 - 20), 100, 40);
                    g.FillRectangle(buttonBrush, deleteButton);
                    g.DrawRectangle(Pens.Black, deleteButton);

                    using (Font buttonFont = new Font("Times New Roman", 12, FontStyle.Bold))
                    {
                        g.DrawString("Delete", buttonFont, Brushes.White, deleteButton, new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center
                        });
                    }

                    startY += rowHeight;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error drawing Products Page: {ex.Message}");
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
            startY += buttonHeight;
            startY += buttonSpacing;
            // Option 2: Remove Product
            Rectangle removeProductButtonRect = new Rectangle(startX, startY , buttonWidth, buttonHeight);
            DrawRoundedButton(g, removeProductButtonRect, "Remove Product");
            startY += buttonHeight;
            startY += buttonSpacing;

            Rectangle updateUsers = new Rectangle(startX, startY, buttonWidth, buttonHeight);
            DrawRoundedButton(g, updateUsers, "update users");
            startY += buttonHeight;
            startY += buttonSpacing;
            Rectangle monitorUserExpression = new Rectangle(startX, startY, buttonWidth, buttonHeight);
            DrawRoundedButton(g, monitorUserExpression, "users expressions");




            // Draw instructions or header
            string headerText = "Admin Menu";
            using (Font headerFont = new Font("Times New Roman", 18, FontStyle.Bold))
            using (Brush headerBrush = new SolidBrush(Color.Black))
            {
                SizeF headerSize = g.MeasureString(headerText, headerFont);
                PointF headerLocation = new PointF((width - headerSize.Width) / 2, 200);
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
            // Draw background image
            using (Image bgImage = Image.FromFile("plain_bk.png"))
            {
                g.DrawImage(bgImage, new Rectangle(0, 0, width, height));
            }

            // Draw the details box
            Rectangle rect = new Rectangle(150, 165, 400, 500);
            Color rectColor = Color.FromArgb(255, 234, 233, 239);
            int cornerRadius = 20;
            DrawRoundedRectangle(g, rect, rectColor, cornerRadius, Color.Green, 1);

            // Product description
            string[] sentences = {
            "Dermatique Facial Wash: Daily purifying formula.",
            "Botanicals cleanse deeply without drying.",
            "Suitable for all skin types, including sensitive.",
            "Use: Apply to damp skin, massage, and rinse."
        };
            Rectangle rect2 = new Rectangle(150, 165, 400, 500);
            Font textFont = new Font("Times New Roman", 14, FontStyle.Regular);
            Brush textBrush = new SolidBrush(Color.FromArgb(100, 100, 100));
            int verticalPadding = 95;
            int horizontalPadding = 20;
            int lineSpacing = 20;
            DrawTextBlock(g, rect2, sentences, textFont, textBrush, verticalPadding, horizontalPadding, lineSpacing);

            // Draw product image
            if (File.Exists(imagePaths[1]))
            {
                using (Image img = Image.FromFile(imagePaths[1]))
                {
                    g.DrawImage(img, new Rectangle(width - 600, 50, 400, 600));
                }
            }

            // Cart quantity and icon
            string cartIconPath = "cartt.png";
            if (File.Exists(cartIconPath))
            {
                using (Image cartIcon = Image.FromFile(cartIconPath))
                {
                    g.DrawImage(cartIcon, new Rectangle(20, 20, 30, 30)); // Adjust position and size as needed
                }
            }

            string cartText = $"Cart: {OveralCart5}";
            using (Font cartFont = new Font("Times New Roman", 16, FontStyle.Bold))
            using (SolidBrush cartBrush = new SolidBrush(Color.Black))
            {
                g.DrawString(cartText, cartFont, cartBrush, new PointF(70, 20));
            }

            // Draw cart icon
           
           

            // Display product price and total
            int price = 20; // Price of the product
            string priceText = $"Price: ${price}";
            string totalPriceText = $"Total: ${OveralCart5 * price}";
            using (Font priceFont = new Font("Times New Roman", 16, FontStyle.Bold))
            using (SolidBrush priceBrush = new SolidBrush(Color.Black))
            {
                g.DrawString(priceText, priceFont, priceBrush, new PointF(20, 60)); // Adjust position as needed
                g.DrawString(totalPriceText, priceFont, priceBrush, new PointF(20, 100)); // Adjust position as needed
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error drawing Dermatique info: " + ex.Message);
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
    private void DrawVitaminInfo(Graphics g)
    {
        int size = height / 10;
        
        
            // Load the background and product images
            Bitmap background = new Bitmap("lightgreen.jpg");
            g.DrawImage(background, new Rectangle(0, 0, width, height));

            Bitmap productImage = new Bitmap("Vitamin.jpeg");
            productImage.MakeTransparent(productImage.GetPixel(0, 0));
            lastObjectImage = productImage; // Store the image as the last known object image

            // Set positions for the image and details
            int staticX = width / 2 - 500; // Static position for the image
            int staticY = height / 2 - 200;
            int newSize = size * 4;

            // Draw the product image
            g.DrawImage(lastObjectImage, new Rectangle(staticX - newSize / 2, staticY - newSize / 2, newSize, newSize));

            // Draw product name and details
            string displayText = "Vitamin D-3";
            Color textColor = Color.Orange;
            using (Brush textBrush = new SolidBrush(textColor))
            {
                g.DrawString(displayText, font, textBrush, new PointF(staticX - 100, staticY + newSize / 2 - 500));
            }

            PointF detailsLocation = new PointF(staticX + 350, staticY + newSize / 2 - 300);
            DrawProductDetails2(g, null, detailsLocation, textColor);

            // Add buttons for increment, decrement, and "Add to Cart"
            int buttonWidth = 120;
            int buttonHeight = 40;
            int marginTop = 60;

            // Decrement button
            Rectangle decrementButton = new Rectangle(staticX - 100, staticY + newSize / 2 + 100, 40, buttonHeight);
            g.FillRectangle(Brushes.LightGray, decrementButton);
            g.DrawRectangle(Pens.Black, decrementButton);
            g.DrawString("-", new Font("Times New Roman", 30), Brushes.Black, decrementButton, new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            });

            // Quantity display box
            Rectangle quantityBox = new Rectangle(staticX - 50, staticY + newSize / 2 + 100, 80, buttonHeight);
            g.FillRectangle(Brushes.White, quantityBox);
            g.DrawRectangle(Pens.Black, quantityBox);
            string cartText = $"{add55}";
            g.DrawString(cartText, new Font("Times New Roman", 28), Brushes.Black, quantityBox, new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            });

            // Increment button
            Rectangle incrementButton = new Rectangle(staticX + 40, staticY + newSize / 2 + 100, 40, buttonHeight);
            g.FillRectangle(Brushes.LightGray, incrementButton);
            g.DrawRectangle(Pens.Black, incrementButton);
            g.DrawString("+", new Font("Times New Roman", 18), Brushes.Black, incrementButton, new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            });

            // Add to Cart button
            Rectangle addToCartButton = new Rectangle(staticX - 70, staticY + newSize / 2 + 40, buttonWidth, buttonHeight);
            g.FillRectangle(Brushes.LightGray, addToCartButton);
            g.DrawRectangle(Pens.Black, addToCartButton);
            g.DrawString("Add To Cart", new Font("Times New Roman", 12, FontStyle.Bold), Brushes.Black, addToCartButton, new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            });

            // Display product price
            int price = 18; // Price of the product
            string priceText = $"Price: ${price}";
            using (Font priceFont = new Font("Times New Roman", 16, FontStyle.Bold))
            using (SolidBrush priceBrush = new SolidBrush(Color.Black))
            {
                g.DrawString(priceText, priceFont, priceBrush, new PointF(staticX - 100, staticY + newSize / 2 + 200)); // Adjust position as needed
            }

            // Draw cart icon
            string cartIconPath = "cartt.png";
            if (File.Exists(cartIconPath))
            {
                using (Image cartIcon = Image.FromFile(cartIconPath))
                {
                    g.DrawImage(cartIcon, new Rectangle(staticX -150, staticY + newSize  + 40, 30, 30)); // Adjust position and size as needed
                }
            }

            // Display total cost
            string totalPriceText = $"Total: ${add55 * price}";
            using (Font totalPriceFont = new Font("Times New Roman", 16, FontStyle.Bold))
            using (SolidBrush totalPriceBrush = new SolidBrush(Color.Black))
            {
                g.DrawString(totalPriceText, totalPriceFont, totalPriceBrush, new PointF(staticX - 100, staticY + newSize / 2 + 240)); // Adjust position as needed
            }
        
    }

    // Method to draw Deodorant information with background
    private void DrawDeodorantInfo(Graphics g)
    {
        int size = height / 10;
        
            // Load the background and deodorant images
            Bitmap background = new Bitmap("Backk.jpg");
            g.DrawImage(background, new Rectangle(0, 0, width, height));

            Bitmap dermatiqueImage = new Bitmap("Deodrant.jpeg");
            dermatiqueImage.MakeTransparent(dermatiqueImage.GetPixel(0, 0));
            lastObjectImage = dermatiqueImage; // Store the image as the last known object image

            // Set positions for the image and details
            int staticX = width / 2 - 500; // Static position for the image
            int staticY = height / 2 - 200;
            int newSize = size * 4;

            // Draw the deodorant image
            g.DrawImage(lastObjectImage, new Rectangle(staticX - newSize / 2, staticY - newSize / 2, newSize, newSize));

            // Draw product name and details
            string displayText = "Degree Cool Rush Deodorant";
            Color textColor = Color.White;
            using (Brush textBrush = new SolidBrush(textColor))
            {
                g.DrawString(displayText, font, textBrush, new PointF(staticX - 200, staticY + newSize / 2 - 500));
            }

            PointF detailsLocation = new PointF(staticX + 500, staticY + newSize / 2 - 300);
            DrawProductDetails(g, null, detailsLocation, textColor);

            // Add buttons for increment, decrement, and "Add to Cart"
            int buttonWidth = 120;
            int buttonHeight = 40;
            int marginTop = 60;

            // Decrement button
            Rectangle decrementButton = new Rectangle(staticX - 100, staticY + newSize / 2 + 100, 40, buttonHeight);
            g.FillRectangle(Brushes.LightGray, decrementButton);
            g.DrawRectangle(Pens.Black, decrementButton);
            g.DrawString("-", new Font("Times New Roman", 30), Brushes.Black, decrementButton, new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            });

            // Quantity display box
            Rectangle quantityBox = new Rectangle(staticX - 50, staticY + newSize / 2 + 100, 80, buttonHeight);
            g.FillRectangle(Brushes.White, quantityBox);
            g.DrawRectangle(Pens.Black, quantityBox);
            string cartText = $"{add45}";
            g.DrawString(cartText, new Font("Times New Roman", 28), Brushes.Black, quantityBox, new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            });

            // Increment button
            Rectangle incrementButton = new Rectangle(staticX + 40, staticY + newSize / 2 + 100, 40, buttonHeight);
            g.FillRectangle(Brushes.LightGray, incrementButton);
            g.DrawRectangle(Pens.Black, incrementButton);
            g.DrawString("+", new Font("Times New Roman", 18), Brushes.Black, incrementButton, new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            });

            // Add to Cart button
            Rectangle addToCartButton = new Rectangle(staticX - 70, staticY + newSize / 2 + 40, buttonWidth, buttonHeight);
            g.FillRectangle(Brushes.LightGray, addToCartButton);
            g.DrawRectangle(Pens.Black, addToCartButton);
            g.DrawString("Add To Cart", new Font("Times New Roman", 12, FontStyle.Bold), Brushes.Black, addToCartButton, new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            });
        
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
            Font openFont = new Font("Times New Roman", 12, FontStyle.Italic | FontStyle.Bold);
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

    private void DrawCrudPage(Graphics g)
    {
        try
        {
            // Background image
            string backgroundPath = "backmenu.png"; // Use the same background as the circular menu
            if (File.Exists(backgroundPath))
            {
                using (Image background = Image.FromFile(backgroundPath))
                {
                    g.DrawImage(background, new Rectangle(0, 0, width, height));
                }
            }

            // Title
            string title = "Your Shopping Cart";
            using (Font titleFont = new Font("Times New Roman", 32, FontStyle.Bold))
            using (Brush titleBrush = new SolidBrush(ColorTranslator.FromHtml("#4a4a4a"))) // Dark gray
            {
                SizeF titleSize = g.MeasureString(title, titleFont);
                PointF titleLocation = new PointF((width - titleSize.Width) / 2, 30);

                // Add shadow for the title
                g.DrawString(title, titleFont, Brushes.White, new PointF(titleLocation.X + 2, titleLocation.Y + 2));
                g.DrawString(title, titleFont, titleBrush, titleLocation);
            }

            // Adjust product quantities if needed
            if (isright)
            {
                if (highlightedProductIndex == 0) OveralCart3++;
                else if (highlightedProductIndex == 1) OveralCart5++;
                else if (highlightedProductIndex == 2) add45++;
                else if (highlightedProductIndex == 3) add55++;
                isright = false;
            }
            if (isleft)
            {
                if (highlightedProductIndex == 0 && OveralCart3 > 0) OveralCart3--;
                else if (highlightedProductIndex == 1 && OveralCart5 > 0) OveralCart5--;
                else if (highlightedProductIndex == 2 && add45 > 0) add45--;
                else if (highlightedProductIndex == 3 && add55 > 0) add55--;
                isleft = false;
            }

            // Product card display
            int startX = 100;
            int startY = 150;
            int spacingY = 220;

            // Product 1: Sunblock
            if (OveralCart3 > 0)
            {
                bool isHighlighted = highlightedProductIndex == 0; // Highlight index 0
                DrawProductCard(g, startX, startY, "Sunblock", OveralCart3, imagePaths[0], isHighlighted);
                if (startY > this.Height - 300)
                {
                    startY = -70;
                    startX += 750;
                }
                startY += spacingY;
            }

            // Product 2: Dermatique
            if (OveralCart5 > 0)
            {
                bool isHighlighted = highlightedProductIndex == 1; // Highlight index 1
                DrawProductCard(g, startX, startY, "Dermatique", OveralCart5, imagePaths[1], isHighlighted);
                if (startY > this.Height - 300)
                {
                    startY = -70;
                    startX += 750;
                }
                startY += spacingY;
            }

            // Product 3: Deodorant
            if (add45 > 0)
            {
                bool isHighlighted = highlightedProductIndex == 2; // Highlight index 2
                DrawProductCard(g, startX, startY, "Degree Cool Rush Deodorant", add45, "Deodrant.jpeg", isHighlighted);
                if (startY > this.Height - 300)
                {
                    startY = -70;
                    startX += 750;
                }
                startY += spacingY;
              
            }

            // Product 4: Vitamin
            if (add55 > 0)
            {
                bool isHighlighted = highlightedProductIndex == 3; // Highlight index 3
                DrawProductCard(g, startX, startY, "Vitamin D-3", add55, "Vitamin.jpeg", isHighlighted);
                if (startY > this.Height - 300)
                {
                    startY = -70;
                    startX += 750;
                }
                startY += spacingY;
            }
       
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error drawing CRUD page: {ex.Message}");
        }
    }



    private void DrawProductCard(Graphics g, int startX, int startY, string productName, int productCount, string productImagePath, bool isHighlighted)
    {
        try
        {
            // Use matching highlight and border colors
            Color cardBorderColor = isHighlighted ? ColorTranslator.FromHtml("#FFA500") : ColorTranslator.FromHtml("#4a4a4a");

            // Card background
            Rectangle cardRect = new Rectangle(startX, startY, 700, 180);
            using (GraphicsPath cardPath = new GraphicsPath())
            {
                int cornerRadius = 20;
                cardPath.AddArc(cardRect.X, cardRect.Y, cornerRadius, cornerRadius, 180, 90);
                cardPath.AddArc(cardRect.Right - cornerRadius, cardRect.Y, cornerRadius, cornerRadius, 270, 90);
                cardPath.AddArc(cardRect.Right - cornerRadius, cardRect.Bottom - cornerRadius, cornerRadius, cornerRadius, 0, 90);
                cardPath.AddArc(cardRect.X, cardRect.Bottom - cornerRadius, cornerRadius, cornerRadius, 90, 90);
                cardPath.CloseFigure();

                using (SolidBrush cardBrush = new SolidBrush(Color.White))
                {
                    g.FillPath(cardBrush, cardPath);
                }

                using (Pen borderPen = new Pen(cardBorderColor, isHighlighted ? 4 : 2))
                {
                    g.DrawPath(borderPen, cardPath);
                }
            }

            // Product image
            if (File.Exists(productImagePath))
            {
                using (Image productImage = Image.FromFile(productImagePath))
                {
                    g.DrawImage(productImage, new Rectangle(startX + 20, startY + 15, 150, 150));
                }
            }

            // Product name and quantity
            string productInfo = $"{productName}\nQuantity: {productCount}";
            using (Font productFont = new Font("Times New Roman", 14, FontStyle.Bold))
            using (Brush productBrush = new SolidBrush(ColorTranslator.FromHtml("#4a4a4a")))
            {
                g.DrawString(productInfo, productFont, productBrush, new PointF(startX + 200, startY + 30));
            }

            // Increment and Decrement buttons
            int buttonSize = 40;
            int buttonX = startX + cardRect.Width - 160;
            int incrementButtonY = startY + 60;
            int decrementButtonY = incrementButtonY + buttonSize + 10;

            // Decrement Button (-)
            Rectangle decrementButtonRect = new Rectangle(buttonX, decrementButtonY, buttonSize, buttonSize);
            g.FillRectangle(Brushes.LightGray, decrementButtonRect);
            g.DrawRectangle(Pens.Black, decrementButtonRect);
            g.DrawString("-", new Font("Times New Roman", 18), Brushes.Black, decrementButtonRect, new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            });

            // Quantity Display Box
            Rectangle quantityBoxRect = new Rectangle(buttonX + buttonSize + 10, incrementButtonY, buttonSize, buttonSize);
            g.FillRectangle(Brushes.White, quantityBoxRect);
            g.DrawRectangle(Pens.Black, quantityBoxRect);
            g.DrawString($"{productCount}", new Font("Times New Roman", 14), Brushes.Black, quantityBoxRect, new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            });

            // Increment Button (+)
            Rectangle incrementButtonRect = new Rectangle(buttonX, incrementButtonY, buttonSize, buttonSize);
            g.FillRectangle(Brushes.LightGray, incrementButtonRect);
            g.DrawRectangle(Pens.Black, incrementButtonRect);
            g.DrawString("+", new Font("Times New Roman", 18), Brushes.Black, incrementButtonRect, new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            });

            // Draw Delete Icon
            string deleteIconPath = "delete.png";
            int deleteIconX = startX + cardRect.Width - 70;
            int deleteIconY = startY + 30;
            if (File.Exists(deleteIconPath))
            {
                using (Image deleteIcon = Image.FromFile(deleteIconPath))
                {
                    g.DrawImage(deleteIcon, new Rectangle(deleteIconX, deleteIconY, buttonSize, buttonSize));
                }
            }

            // Draw Update Icon
            string updateIconPath = "update.png";
            int updateIconY = deleteIconY + buttonSize + 10;
            if (File.Exists(updateIconPath))
            {
                using (Image updateIcon = Image.FromFile(updateIconPath))
                {
                    g.DrawImage(updateIcon, new Rectangle(deleteIconX, updateIconY, buttonSize, buttonSize));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error displaying product card: {ex.Message}");
        }
    }





    private void DrawModernButton(Graphics g, Rectangle buttonRect, string buttonText, Color buttonColor)
    {
        using (GraphicsPath buttonPath = new GraphicsPath())
        {
            int cornerRadius = 10;
            buttonPath.AddArc(buttonRect.X, buttonRect.Y, cornerRadius, cornerRadius, 180, 90);
            buttonPath.AddArc(buttonRect.Right - cornerRadius, buttonRect.Y, cornerRadius, cornerRadius, 270, 90);
            buttonPath.AddArc(buttonRect.Right - cornerRadius, buttonRect.Bottom - cornerRadius, cornerRadius, cornerRadius, 0, 90);
            buttonPath.AddArc(buttonRect.X, buttonRect.Bottom - cornerRadius, cornerRadius, cornerRadius, 90, 90);
            buttonPath.CloseFigure();

            using (LinearGradientBrush buttonBrush = new LinearGradientBrush(
                buttonRect,
                buttonColor,
                Color.White,
                LinearGradientMode.Vertical))
            {
                g.FillPath(buttonBrush, buttonPath);
            }

            using (Pen borderPen = new Pen(Color.DarkGray, 2))
            {
                g.DrawPath(borderPen, buttonPath);
            }

            using (Font buttonFont = new Font("Times New Roman", 12, FontStyle.Bold))
            using (Brush textBrush = new SolidBrush(Color.White))
            {
                SizeF textSize = g.MeasureString(buttonText, buttonFont);
                PointF textLocation = new PointF(
                    buttonRect.X + (buttonRect.Width - textSize.Width) / 2,
                    buttonRect.Y + (buttonRect.Height - textSize.Height) / 2);
                g.DrawString(buttonText, buttonFont, textBrush, textLocation);
            }
        }
    }


    private void DisplayProductWithButtons(Graphics g, int startX, int startY, string productName, int productCount, string productImagePath)
    {
        try
        {
            // Draw product image
            if (File.Exists(productImagePath))
            {
                using (Image productImage = Image.FromFile(productImagePath))
                {
                    g.DrawImage(productImage, new Rectangle(startX, startY, 150, 150)); // Adjust size and position
                }
            }

            // Draw product name and count
            string productInfo = $"{productName}\nCount: {productCount}";
            using (Font infoFont = new Font("Times New Roman", 14))
            using (Brush infoBrush = new SolidBrush(Color.Black))
            {
                g.DrawString(productInfo, infoFont, infoBrush, new PointF(startX + 180, startY + 30)); // Adjust position
            }

            // Draw "Delete" button
            Rectangle deleteButtonRect = new Rectangle(startX + 350, startY + 30, 100, 40);
            DrawRoundedButton(g, deleteButtonRect, "Delete");

            // Draw "Update" button
            Rectangle updateButtonRect = new Rectangle(startX + 350, startY + 100, 100, 40);
            DrawRoundedButton(g, updateButtonRect, "Update");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error displaying product: {ex.Message}");
        }
    }

    private void DisplayProductInfo(Graphics g, int startX, int startY, string productName, int productCount, string productImagePath)
    {
        // Display product image
        if (File.Exists(productImagePath))
        {
            using (Image productImage = Image.FromFile(productImagePath))
            {
                g.DrawImage(productImage, new Rectangle(startX, startY, 150, 150)); // Adjust size and position
            }
        }

        // Display product name and count
        string productInfo = $"{productName}\nCount: {productCount}";
        using (Font infoFont = new Font("Times New Roman", 14))
        using (Brush infoBrush = new SolidBrush(Color.Black))
        {
            g.DrawString(productInfo, infoFont, infoBrush, new PointF(startX + 200, startY + 50)); // Adjust position
        }
    }


    private void DrawButton(Graphics g, Rectangle buttonRect, string buttonText, Color buttonColor)
    {
        using (GraphicsPath path = new GraphicsPath())
        {
            int cornerRadius = 15;
            path.AddArc(buttonRect.X, buttonRect.Y, cornerRadius, cornerRadius, 180, 90);
            path.AddArc(buttonRect.X + buttonRect.Width - cornerRadius, buttonRect.Y, cornerRadius, cornerRadius, 270, 90);
            path.AddArc(buttonRect.X + buttonRect.Width - cornerRadius, buttonRect.Y + buttonRect.Height - cornerRadius, cornerRadius, cornerRadius, 0, 90);
            path.AddArc(buttonRect.X, buttonRect.Y + buttonRect.Height - cornerRadius, cornerRadius, cornerRadius, 90, 90);
            path.CloseAllFigures();

            // Fill button
            using (SolidBrush brush = new SolidBrush(buttonColor))
            {
                g.FillPath(brush, path);
            }

            // Draw border
            using (Pen pen = new Pen(Color.Black, 2))
            {
                g.DrawPath(pen, path);
            }

            // Draw text
            using (Font font = new Font("Times New Roman", 12, FontStyle.Bold))
            using (SolidBrush textBrush = new SolidBrush(Color.Black))
            {
                SizeF textSize = g.MeasureString(buttonText, font);
                PointF textLocation = new PointF(
                    buttonRect.X + (buttonRect.Width - textSize.Width) / 2,
                    buttonRect.Y + (buttonRect.Height - textSize.Height) / 2
                );
                g.DrawString(buttonText, font, textBrush, textLocation);
            }
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
            using (Font buttonFont = new Font("Times New Roman", 12, FontStyle.Bold))
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
    private void LoadUserProfile(string userName)
    {
        try
        {
            using (var package = new ExcelPackage(new FileInfo(excelFilePath)))
            {
                // Get the worksheet by name (update "Sheet1" to match your sheet name)
                var worksheet = package.Workbook.Worksheets["Sheet1"];

                // Check if the worksheet is empty
                if (worksheet.Dimension == null)
                {
                    MessageBox.Show("The worksheet is empty.");
                    return;
                }

                // Get the total rows and columns
                int totalRows = worksheet.Dimension.Rows;

                bool userFound = false;

                // Iterate through each row
                for (int row = 2; row <= totalRows; row++) // Assuming row 1 contains headers
                {
                    string nameInRow = worksheet.Cells[row, 1].Text; // Column 1 contains names

                    if (nameInRow.Equals(userName, StringComparison.OrdinalIgnoreCase))
                    {
                        userFound = true;

                        // Fetch relevant user data
                        string product = worksheet.Cells[row, 2].Text;  // Column 2: Product
                        string quantity = worksheet.Cells[row, 3].Text; // Column 3: Quantity
                        string emotion = worksheet.Cells[row, 4].Text;  // Column 4: Emotion

                        // Display or process the data
                        MessageBox.Show($"Name: {nameInRow}\nProduct: {product}\nQuantity: {quantity}\nEmotion: {emotion}");
                    }
                }

                if (!userFound)
                {
                    MessageBox.Show($"No data found for user: {userName}");
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error reading the Excel file: {ex.Message}");
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
    private void Logout()
    {
        
        currentUserName = null;
        userCart.Clear();
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

                                string[] names = { "tarek", "farah", "youssef", "malak", "roqaia", "rawan" };

                                if (message.ToLower().Contains("vitamin"))
                                {
                                    // Show message box for vitamin
                                    MessageBox.Show("Received message: Vitamin detected! Displaying Vitamin Info...");
                                    //latestId = 55;
                                    // Call a method to draw the information about Vitamin
                                    //Invoke(new Action(() => DrawVitaminInfo(Graphics.FromHwnd(this.Handle))));
                                    //latestId = 55;

                                }


                                else if (message.ToLower().Contains("deodorant"))
                                {
                                    // Show message box for deodorant
                                    MessageBox.Show("Received message: Deodorant detected! Displaying Deodorant Info...");
                                    // Call a method to draw the information about Deodorant
                                    Invoke(new Action(() => DrawDeodorantInfo(Graphics.FromHwnd(this.Handle))));
                                   // latestId = 45;
                                    
                                }
                                else if (message.ToLower().Contains("right"))
                                {
                                    isright= true;
                                    MessageBox.Show("Received message: object detection! right...");
                                   // Invoke(new Action(() => DrawVitaminInfo(Graphics.FromHwnd(this.Handle))));

                                    if (latestId == 3)
                                    {
                                        OveralCart3++;
                                        if (OveralCart3 == 1)
                                        {
                                            OveralCart++;

                                        }
                                    }
                                    if (latestId == 5)
                                    {
                                        OveralCart5++;
                                        if (OveralCart5 == 1)
                                        {
                                            OveralCart++;

                                        }
                                    }
                                    if (latestId == 45)
                                    {
                                        add45++;
                                        if (add45 == 1)
                                        {
                                            OveralCart++;

                                        }
                                    }
                                    add55++;
                                    if (add55 == 1)
                                    {
                                        OveralCart++;

                                    }


                                }
                                else if (message.ToLower().Contains("left"))
                                {
                                    //  Invoke(new Action(() => DrawVitaminInfo(Graphics.FromHwnd(this.Handle))));

                                    isleft = true;
                                    //MessageBox.Show("Received message: Gesture detected! left...");
                                    MessageBox.Show("Received message: object detection! left...");

                                    if (latestId == 3)
                                    {
                                        if (OveralCart3 != 0)
                                        {
                                            OveralCart3--;
                                            if (OveralCart3 == 0)
                                            {
                                                OveralCart--;

                                            }

                                        }
                                    }
                                    if (latestId == 5)
                                    {
                                        if (OveralCart5 != 0)
                                        {
                                            OveralCart5--;
                                            if (OveralCart5 == 0)
                                            {
                                                OveralCart--;

                                            }

                                        }
                                    }
                                    if (latestId == 45)
                                    {
                                        if (add45 != 0)
                                        {
                                            add45--;
                                            if (add45 == 0)
                                            {
                                                OveralCart--;

                                            }

                                        }

                                    }
                                    if (add55 != 0)
                                    {
                                        add55--;
                                        if (add55 == 0)
                                        {
                                            OveralCart--;

                                        }

                                    }

                                }
                                if (latestId == 4)
                                {

                                    if (facelogin == false)
                                    {

                                        foreach (var name in names)
                                        {
                                            if (message.ToLower().Contains(name))
                                            {
                                                currentUserName = name;
                                                
                                                LoadUserProfile(currentUserName);
                           
                                                facelogin = true;
                                                // Show the complete message in a MessageBox
                                                MessageBox.Show($"Complete Message: {message}");
                                                MessageBox.Show($"Welcome: {name}");
                                                latestId = 12;
                                               // Invoke(new Action(() => DrawCrudPage(Graphics.FromHwnd(this.Handle))));
                                                Invoke(new Action(() => DrawCircularMenu(Graphics.FromHwnd(this.Handle),width/2,height/2,300,-1)));

                                                break; // Exit the loop once a match is found
                                            }
                                        }
                                    }
                                }
                                

                                // Check if the message contains "customer"
                                if (message.ToLower().Contains("customer") && customer == false)
                                {
                                    // Show message box for customer
                                    MessageBox.Show("Received message: Customer detected! Displaying Menu...");

                                    // Set the flag for customer
                                    customer = true;

                                    // Call method to draw the menu items
                                    // We can call it with some default values for now, adjust parameters as needed
                                    Invoke(new Action(() => DrawMenuItems(Graphics.FromHwnd(this.Handle), 0, true, Color.Blue)));
                                }
                                else
                                {
                                    if (message.ToLower().Contains("admin"))
                                    {
                                        // Handle any other message
                                        if (admin == false)
                                        {
                                            admin = true;
                                            MessageBox.Show($"Received message:admin detected! Displaying Menu.. ");
                                            Invoke(new Action(() => ShowAdminMenu(Graphics.FromHwnd(this.Handle))));
                                        }
                                    }
                                }
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


    private void ProcessLoginMessage(string message)
    {
        if (!messageDisplayed)
        {
            messageDisplayed = true;

            this.Invoke(new Action(() =>
            {
                ShowMessageOnScreen("Loading... Searching for user", Color.Orange);
            }));

            Task.Run(async () =>
            {
                await Task.Delay(5000);

                this.Invoke(new Action(() =>
                {
                    ShowMessageOnScreen(message, Color.Blue);
                    MessageBox.Show(message, "Login", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }));
            });
        }
    }
   

    private void ShowMessageOnScreen(string text, Color color)
    {
        Label messageLabel = new Label
        {
            Text = text,
            ForeColor = color,
            Font = new Font("Times New Roman", 18, FontStyle.Bold),
            AutoSize = true,
            BackColor = Color.Transparent,
            Location = new Point((this.Width - 300) / 2, (this.Height - 50) / 2)
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





}
