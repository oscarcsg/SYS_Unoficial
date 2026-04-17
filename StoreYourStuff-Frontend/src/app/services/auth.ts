// /src/app/services/auth.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { LoginRequest, LoginResponse, RegisterRequest } from '../models/auth.model';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly API_URL = 'http://localhost:5000/api';

  constructor(private http: HttpClient) {}

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.API_URL}/users/login`, credentials).pipe(
      // El operador 'tap' intercepta la respuesta y guardar el token en el navegador
      // de forma silenciosa antes de devolver los datos al componente
      tap((response) => {
        if (response.token) {
          localStorage.setItem('jwt_token', response.token);
          localStorage.setItem('user_data', JSON.stringify(response.userData));
        }
      }),
    );
  }

  register(credentials: RegisterRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.API_URL}/users`, credentials);
  }
}
