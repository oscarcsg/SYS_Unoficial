import { Component } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './login.html',
})
export class LoginComponent {
  loginForm: FormGroup;
  errorMessage: string = '';

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
  ) {
    this.loginForm = this.fb.group({
      // Un unico campo, para alias o email
      identificador: ['', [Validators.required]],
      password: ['', Validators.required],
    });
  }

  onSubmit() {
    if (this.loginForm.invalid) return;

    this.errorMessage = '';
    const formValue = this.loginForm.value;

    // Detectar si el usuario ha escrito un email (contiene @) o un alias
    const isEmail = formValue.identificador.includes('@');

    const credentials = {
      email: isEmail ? formValue.identificador : undefined,
      alias: !isEmail ? formValue.identificador : undefined,
      password: formValue.password,
    };

    // Llamar al backend
    this.authService.login(credentials).subscribe({
      next: (response) => {
        console.log('¡Login exitoso!', response);
        alert('¡Bienvenido a StoreYourStuff, ' + response.userData.alias + '!');
      },
      error: (err) => {
        console.error('Error en el login', err);
        this.errorMessage = 'Credenciales incorrectas.';
      },
    });
  }
}
