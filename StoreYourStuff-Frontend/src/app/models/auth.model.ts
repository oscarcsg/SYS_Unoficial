export interface LoginRequest {
  email?: string;
  alias?: string;
  password: string;
}

export interface RegisterRequest {
  alias: string;
  email: string;
  password: string;
}

export interface UserData {
  id: number;
  alias: string;
  email: string;
  createdAt: string;
}

export interface LoginResponse {
  message: string;
  token: string;
  userData: UserData;
}
