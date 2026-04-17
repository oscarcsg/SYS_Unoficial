// /src/app/app.routes.ts
import { Routes } from '@angular/router';
import { LoginComponent } from './pages/login/login';
import { RegisterComponent } from './pages/register/register';

export const routes: Routes = [
  { path: '', component: LoginComponent }, // Ruta raíz (http://localhost:4200)
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
];
