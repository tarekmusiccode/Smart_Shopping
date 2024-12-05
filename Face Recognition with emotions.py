import face_recognition
import cv2
import numpy as np
from deepface import DeepFace

# Get a reference to webcam #0 (the default one)
video_capture = cv2.VideoCapture(0)

# Load a sample picture and learn how to recognize it.
malak_image = face_recognition.load_image_file("images/Malak.jpg")
malak_face_encoding = face_recognition.face_encodings(malak_image)[0]

# Load a second sample picture and learn how to recognize it.
farah_image = face_recognition.load_image_file("images/Farah.jpg")
farah_face_encoding = face_recognition.face_encodings(farah_image)[0]


# Load a second sample picture and learn how to recognize it.
tarek_image = face_recognition.load_image_file("images/Tarek.jpg")
tarek_face_encoding = face_recognition.face_encodings(tarek_image)[0]


# Load a second sample picture and learn how to recognize it.
youssef_image = face_recognition.load_image_file("images/Youssef.jpg")
youssef_face_encoding = face_recognition.face_encodings(youssef_image)[0]


# Load a second sample picture and learn how to recognize it.
rawan_image = face_recognition.load_image_file("images/Rawan.jpg")
rawan_face_encoding = face_recognition.face_encodings(rawan_image)[0]


# Load a second sample picture and learn how to recognize it.
rokaia_image = face_recognition.load_image_file("images/Rokaia.jpg")
rokaia_face_encoding = face_recognition.face_encodings(rokaia_image)[0]


# Load a second sample picture and learn how to recognize it.
drmoamen_image = face_recognition.load_image_file("images/Eng. Moamen Zaher.jpg")
drmoamen_face_encoding = face_recognition.face_encodings(drmoamen_image)[0]


# Load a second sample picture and learn how to recognize it.
drmohamed_image = face_recognition.load_image_file("images/Eng. Mohamed Ashraf.jpg")
drmohamed_face_encoding = face_recognition.face_encodings(drmohamed_image)[0]


# Load a second sample picture and learn how to recognize it.
drfarah_image = face_recognition.load_image_file("images/Eng. Farah Darwish.jpg")
drfarah_face_encoding = face_recognition.face_encodings(drfarah_image)[0]


# Load a second sample picture and learn how to recognize it.
drayman_image = face_recognition.load_image_file("images/Dr. Ayman Ezzat.jpg")
drayman_face_encoding = face_recognition.face_encodings(drayman_image)[0]


# Create arrays of known face encodings and their names
known_face_encodings = [
    malak_face_encoding,
    farah_face_encoding,
    rawan_face_encoding,
    rokaia_face_encoding,
    drayman_face_encoding,
    drfarah_face_encoding,
    drmohamed_face_encoding,
    drmoamen_face_encoding,
    tarek_face_encoding,
    youssef_face_encoding,
]
known_face_names = [
    "Malak",
    "Farah",
    "Rawan",
    "Rokaia",
    "Tarek",
    "Youssef",
    "Eng. Moamen Zaher",
    "Eng. Mohamed Ashraf",
    "Eng. Farah Darwish",
    "Dr. Ayman Ezzat"
]

# Initialize some variables
face_locations = []
face_encodings = []
face_names = []
process_this_frame = True

while True:
    # Grab a single frame of video
    ret, frame = video_capture.read()

    # Flip the frame horizontally for a mirror effect
    flipped_frame = cv2.flip(frame, 1)

    # Only process every other frame of video to save time
    if process_this_frame:
        # Resize frame of video to 1/4 size for faster face recognition processing
        small_frame = cv2.resize(flipped_frame, (0, 0), fx=0.25, fy=0.25)

        # Convert the image from BGR color (which OpenCV uses) to RGB color
        rgb_small_frame = cv2.cvtColor(small_frame, cv2.COLOR_BGR2RGB)

        # Find all the faces and face encodings in the current frame of video
        face_locations = face_recognition.face_locations(rgb_small_frame)
        face_encodings = face_recognition.face_encodings(rgb_small_frame, face_locations)

        face_names = []
        for face_encoding in face_encodings:
            # See if the face is a match for the known face(s)
            matches = face_recognition.compare_faces(known_face_encodings, face_encoding)
            name = "Unknown"

            # Or instead, use the known face with the smallest distance to the new face
            face_distances = face_recognition.face_distance(known_face_encodings, face_encoding)
            best_match_index = np.argmin(face_distances)
            if matches[best_match_index]:
                name = known_face_names[best_match_index]

            face_names.append(name)

    process_this_frame = not process_this_frame

    # Display the results
    rgb_frame = cv2.cvtColor(flipped_frame, cv2.COLOR_BGR2RGB)  # Convert the frame for emotion analysis

    for (top, right, bottom, left), name in zip(face_locations, face_names):
        # Scale back up face locations since the frame we detected in was scaled to 1/4 size
        top *= 4
        right *= 4
        bottom *= 4
        left *= 4

        # Draw a box around the face
        cv2.rectangle(flipped_frame, (left, top), (right, bottom), (0, 0, 255), 2)

        # Extract face ROI for emotion detection
        face_roi = rgb_frame[top:bottom, left:right]
        try:
            emotion_result = DeepFace.analyze(face_roi, actions=['emotion'], enforce_detection=False)
            emotion = emotion_result[0]['dominant_emotion']
        except Exception as e:
            emotion = "Unknown"

        # Draw a label with a name and emotion below the face
        label = f"{name} ({emotion})"
        cv2.rectangle(flipped_frame, (left, bottom - 35), (right, bottom), (0, 0, 255), cv2.FILLED)
        font = cv2.FONT_HERSHEY_DUPLEX
        cv2.putText(flipped_frame, label, (left + 6, bottom - 6), font, 1.0, (255, 255, 255), 1)

    # Display the resulting image
    cv2.imshow('Video', flipped_frame)

    # Hit 'q' on the keyboard to quit!
    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

# Release handle to the webcam
video_capture.release()
cv2.destroyAllWindows()
