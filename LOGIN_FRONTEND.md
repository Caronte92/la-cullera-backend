# Instrucciones para implementar el login en el frontend

## 1. Formulario de Login
- Crea un formulario con campos para **usuario/email** y **contraseña**.
- Añade validación básica (campos obligatorios, formato de email, etc).

## 2. Petición al backend
- Cuando el usuario envíe el formulario, haz una petición **POST** a tu endpoint de login, por ejemplo:
  ```http
  POST http://localhost:9000/auth/login
  Content-Type: application/json

  {
    "username": "usuario",
    "password": "contraseña"
  }
  ```
- El backend responderá con un **JWT** (y posiblemente un refresh token).

## 3. Guardar el token
- Guarda el **JWT** en el **localStorage** o **sessionStorage** del navegador:
  ```js
  localStorage.setItem('token', jwt);
  ```
- **Nunca** guardes el token en variables globales ni en cookies sin HttpOnly.

## 4. Enviar el token en cada petición
- Para acceder a rutas protegidas, añade el header Authorization en cada petición:
  ```http
  Authorization: Bearer <token>
  ```
- Si usas fetch:
  ```js
  fetch('/ruta-protegida', {
    headers: {
      'Authorization': 'Bearer ' + localStorage.getItem('token')
    }
  })
  ```
- Si usas Axios, puedes configurar un interceptor global.

## 5. Controlar el acceso en el frontend
- Antes de mostrar páginas protegidas, verifica si el token existe y es válido (puedes decodificarlo con jwt-decode para ver la expiración).
- Si no hay token o está expirado, redirige al login.

## 6. Cerrar sesión
- Borra el token del storage y redirige al login.

## 7. Manejo de errores
- Si el backend responde 401/403, muestra un mensaje de error y/o redirige al login.

---

## Resumen de flujo
1. Usuario envía login → recibe JWT.
2. Frontend guarda JWT.
3. Frontend envía JWT en cada petición protegida.
4. Si el backend responde 401, frontend borra el token y pide login de nuevo.

---

¿Necesitas un ejemplo de código en React, Vue o algún framework concreto? ¿O también la gestión de refresh tokens? Pídelo si lo necesitas.
