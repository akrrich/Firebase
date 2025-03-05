using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using System.Collections;
using TMPro;

public class FirebaseAuthManager : MonoBehaviour
{
    private FirebaseAuth auth;
    private DatabaseReference databaseRef;

    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField usernameInput;


    void Awake()
    {
        // Verificar si Firebase está correctamente configurado
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            DependencyStatus status = task.Result;
            if (status == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                databaseRef = FirebaseDatabase.DefaultInstance.RootReference;

                Debug.Log("Firebase inicializado correctamente.");
            }

            else
            {
                Debug.LogError($"No se pudo inicializar Firebase: {status}");
            }
        });
    }


    // Función llamada cuando se presiona el botón de registro
    public void OnRegisterButtonClicked()
    {
        string email = emailInput.text;
        string password = passwordInput.text;
        string username = usernameInput.text;

        // Validar los campos antes de registrar
        if (email != "" && password.Length >= 6 && username != "")
        {
            RegisterUser(email, password, username);
        }
        else
        {
            Debug.LogError("Por favor, completa todos los campos correctamente.");
        }
    }

    // Función llamada cuando se presiona el botón de incio de sesion
    public void OnLoginButtonClicked()
    {
        string email = emailInput.text;
        string password = passwordInput.text;

        // Validar los campos antes de iniciar sesión
        if (email != "" && password.Length >= 6)
        {
            LoginUser(email, password);
        }
        else
        {
            Debug.LogError("Por favor, ingresa un correo electrónico y una contraseña válidos.");
        }
    }

    private void LoginUser(string email, string password)
    {
        StartCoroutine(LoginCoroutine(email, password));
    }

    // Corutina para iniciar sesión
    private IEnumerator LoginCoroutine(string email, string password)
    {
        var loginTask = auth.SignInWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => loginTask.IsCompleted);

        if (loginTask.Exception != null)
        {
            Debug.LogError($"Error al iniciar sesión: {loginTask.Exception}");
        }
        else
        {
            FirebaseUser user = loginTask.Result.User;

            // Verificar si el correo electrónico está verificado
            if (user.IsEmailVerified)
            {
                Debug.Log("Inicio de sesión exitoso!");
                // Aquí puedes continuar con el flujo de inicio de sesión, como cargar la siguiente escena
            }
            else
            {
                Debug.LogError("El correo electrónico no ha sido verificado. Por favor, verifica tu correo.");
                // Puedes mostrar un mensaje de error al usuario indicando que debe verificar su correo
            }
        }
    }

    // Función para registrar un usuario
    private void RegisterUser(string email, string password, string username)
    {
        StartCoroutine(RegisterCoroutine(email, password, username));
    }

    // Corutina para registrar al usuario
    private IEnumerator RegisterCoroutine(string email, string password, string username)
    {
        var registerTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => registerTask.IsCompleted);

        if (registerTask.Exception != null)
        {
            Debug.LogError($"Error al registrar usuario: {registerTask.Exception}");
        }
        else
        {
            FirebaseUser newUser = registerTask.Result.User;
            Debug.Log("Usuario registrado con éxito!");

            // Enviar correo de verificación
            StartCoroutine(SendVerificationEmail(newUser));

            // Guardar el tiempo en que se registró el usuario
            float startTime = Time.time;

            while (!newUser.IsEmailVerified && Time.time - startTime < 20f) 
            {
                yield return null; // Esperar el siguiente frame
            }

            // Si no se verificó el correo dentro de 2 minutos, eliminar el usuario
            if (!newUser.IsEmailVerified)
            {
                Debug.LogError("El usuario no verificó su correo a tiempo. Eliminando usuario.");
                DeleteUser(newUser);
            }
            else
            {
                // Después de la verificación, guardar al usuario en la base de datos
                SaveUserToDatabase(newUser.UserId, username, email);
                Debug.Log("Usuario guardado en la base de datos después de la verificación del correo.");
            }
        }
    }

    // Función para guardar al usuario en la base de datos
    private void SaveUserToDatabase(string userId, string username, string email)
    {
        User newUser = new User(username, email);
        string json = JsonUtility.ToJson(newUser);
        databaseRef.Child("users").Child(userId).SetRawJsonValueAsync(json);
    }

    private void DeleteUser(FirebaseUser user)
    {
        // Eliminar el usuario de la base de datos
        databaseRef.Child("users").Child(user.UserId).RemoveValueAsync();

        // Eliminar el usuario de Firebase
        user.DeleteAsync().ContinueWith(task =>
        {
            if (task.Exception != null)
            {
                Debug.LogError($"Error al eliminar usuario: {task.Exception}");
            }
            else
            {
                Debug.Log("Usuario eliminado correctamente.");
            }
        });
    }

    // Corutina para enviar el correo de verificación
    private IEnumerator SendVerificationEmail(FirebaseUser user)
    {
        var emailTask = user.SendEmailVerificationAsync();
        yield return new WaitUntil(() => emailTask.IsCompleted);

        if (emailTask.Exception != null)
        {
            Debug.LogError($"Error al enviar verificación: {emailTask.Exception}");
        }
        else
        {
            Debug.Log("Correo de verificación enviado!");
        }
    }
}

[System.Serializable]
public class User
{
    public string email;
    public string username;

    public User(string username, string email)
    {
        this.username = username;
        this.email = email;
    }
}
