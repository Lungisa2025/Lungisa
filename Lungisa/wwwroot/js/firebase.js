// Import the functions you need from the SDKs you need
import { initializeApp } from "firebase/app";
import { getAnalytics } from "firebase/analytics";
// TODO: Add SDKs for Firebase products that you want to use
// https://firebase.google.com/docs/web/setup#available-libraries

// Your web app's Firebase configuration
// For Firebase JS SDK v7.20.0 and later, measurementId is optional
// For Firebase JS SDK v7.20.0 and later, measurementId is optional
const firebaseConfig = {
    apiKey: "AIzaSyCpvmanPzoY_Tno21lvGxTKvmnuphjyvcs",
    authDomain: "lungisa-9d510.firebaseapp.com",
    databaseURL: "https://lungisa-9d510-default-rtdb.firebaseio.com",
    projectId: "lungisa-9d510",
    storageBucket: "lungisa-9d510.firebasestorage.app",
    messagingSenderId: "891047703840",
    appId: "1:891047703840:web:e4d5a32c0cbbe293411345",
    measurementId: "G-1QERMZGNVY"
};



// Initialize Firebase
const app = initializeApp(firebaseConfig);
const analytics = getAnalytics(app);


export default app;