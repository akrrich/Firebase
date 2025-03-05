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

    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField usernameInput;


    void Awake()
    {
        InitializeFirebase();
    }


    // Funci�n llamada cuando se presiona el bot�n de registro
    public void OnRegisterButtonClicked()
    {
        string email = emailInput.text;
        string password = passwordInput.text;
        string username = usernameInput.text;

        // Validacion provisoria
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

        // Validacion provisoria
        if (email != "" && password.Length >= 6)
        {
            LoginUser(email, password);
        }
        else
        {
            Debug.LogError("Por favor, ingresa un correo electr�nico y una contrase�a v�lidos.");
        }
    }


    private void InitializeFirebase()
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

    private void LoginUser(string email, string password)
    {
        StartCoroutine(LoginCoroutine(email, password));
    }

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

            if (user.IsEmailVerified)
            {
                Debug.Log("Inicio de sesi�n exitoso!");
                // continuar con el flujo de inicio de sesi�n
            }
            else
            {
                Debug.LogError("El correo electr�nico no ha sido verificado. Por favor, verifica tu correo.");
            }
        }
    }

    private void RegisterUser(string email, string password, string username)
    {
        StartCoroutine(RegisterCoroutine(email, password, username));
    }

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
            Debug.Log("Verifique su correo electronico!");

            StartCoroutine(SendVerificationEmail(newUser));

            float startTime = Time.time;

            while (!newUser.IsEmailVerified && Time.time - startTime < 120f) 
            {
                yield return null; // Esperar el siguiente frame
            }

            if (!newUser.IsEmailVerified)
            {
                DeleteUserFromDatabase(newUser);
                Debug.LogError("El usuario no verific� su correo a tiempo. Eliminando usuario.");
            }
            else
            {
                SaveUserToDatabase(newUser.UserId, username, email);
                Debug.Log("Usuario guardado en la base de datos despu�s de la verificaci�n del correo.");
            }
        }
    }

    private void SaveUserToDatabase(string userId, string username, string email)
    {
        User newUser = new User(username, email);
        string json = JsonUtility.ToJson(newUser);
        databaseRef.Child("users").Child(userId).SetRawJsonValueAsync(json);
    }

    private void DeleteUserFromDatabase(FirebaseUser user)
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
