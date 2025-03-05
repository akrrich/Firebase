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
        // Verificar si Firebase est� correctamente configurado
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


    // Funci�n llamada cuando se presiona el bot�n de registro
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

    // Funci�n llamada cuando se presiona el bot�n de incio de sesion
    public void OnLoginButtonClicked()
    {
        string email = emailInput.text;
        string password = passwordInput.text;

        // Validar los campos antes de iniciar sesi�n
        if (email != "" && password.Length >= 6)
        {
            LoginUser(email, password);
        }
        else
        {
            Debug.LogError("Por favor, ingresa un correo electr�nico y una contrase�a v�lidos.");
        }
    }

    private void LoginUser(string email, string password)
    {
        StartCoroutine(LoginCoroutine(email, password));
    }

    // Corutina para iniciar sesi�n
    private IEnumerator LoginCoroutine(string email, string password)
    {
        var loginTask = auth.SignInWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => loginTask.IsCompleted);

        if (loginTask.Exception != null)
        {
            Debug.LogError($"Error al iniciar sesi�n: {loginTask.Exception}");
        }
        else
        {
            FirebaseUser user = loginTask.Result.User;

            // Verificar si el correo electr�nico est� verificado
            if (user.IsEmailVerified)
            {
                Debug.Log("Inicio de sesi�n exitoso!");
                // Aqu� puedes continuar con el flujo de inicio de sesi�n, como cargar la siguiente escena
            }
            else
            {
                Debug.LogError("El correo electr�nico no ha sido verificado. Por favor, verifica tu correo.");
                // Puedes mostrar un mensaje de error al usuario indicando que debe verificar su correo
            }
        }
    }

    // Funci�n para registrar un usuario
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
            Debug.Log("Usuario registrado con �xito!");

            // Enviar correo de verificaci�n
            StartCoroutine(SendVerificationEmail(newUser));

            // Guardar el tiempo en que se registr� el usuario
            float startTime = Time.time;

            while (!newUser.IsEmailVerified && Time.time - startTime < 20f) 
            {
                yield return null; // Esperar el siguiente frame
            }

            // Si no se verific� el correo dentro de 2 minutos, eliminar el usuario
            if (!newUser.IsEmailVerified)
            {
                Debug.LogError("El usuario no verific� su correo a tiempo. Eliminando usuario.");
                DeleteUser(newUser);
            }
            else
            {
                // Despu�s de la verificaci�n, guardar al usuario en la base de datos
                SaveUserToDatabase(newUser.UserId, username, email);
                Debug.Log("Usuario guardado en la base de datos despu�s de la verificaci�n del correo.");
            }
        }
    }

    // Funci�n para guardar al usuario en la base de datos
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

    // Corutina para enviar el correo de verificaci�n
    private IEnumerator SendVerificationEmail(FirebaseUser user)
    {
        var emailTask = user.SendEmailVerificationAsync();
        yield return new WaitUntil(() => emailTask.IsCompleted);

        if (emailTask.Exception != null)
        {
            Debug.LogError($"Error al enviar verificaci�n: {emailTask.Exception}");
        }
        else
        {
            Debug.Log("Correo de verificaci�n enviado!");
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
