import cv2
from deepface import DeepFace
from simple_facerec import SimpleFacerec

sfr = SimpleFacerec()
sfr.load_encoding_images("images/")  

cap = cv2.VideoCapture(0)
while True:
    ret, frame = cap.read()
    if not ret:
        break

    flipped_frame = cv2.flip(frame, 1)  
    rgb_frame = cv2.cvtColor(flipped_frame, cv2.COLOR_BGR2RGB)  
    face_locations, face_names = sfr.detect_known_faces(flipped_frame)
    for face_loc, name in zip(face_locations, face_names):
        y1, x2, y2, x1 = face_loc[0], face_loc[1], face_loc[2], face_loc[3]
        
        #crop face region
        face_roi = rgb_frame[y1:y2, x1:x2]
        try:
            emotion_result = DeepFace.analyze(face_roi, actions=['emotion'], enforce_detection=False)
            emotion = emotion_result[0]['dominant_emotion']
        except Exception as e:
            emotion = "Unknown" 

        display_text = f"{name} is {emotion}"
        cv2.putText(flipped_frame, display_text, (x1, y1 - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.9, (0, 0, 200), 2)
        cv2.rectangle(flipped_frame, (x1, y1), (x2, y2), (0, 0, 200), 2)
        
    cv2.imshow("Face rec with emotion", flipped_frame)
    key = cv2.waitKey(1)
    if key == 27 :
        break



cap.release()
cv2.destroyAllWindows()
