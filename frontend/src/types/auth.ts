export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
  wirdId: string;
  emailConfirmed: boolean;
  createdAtUtc: string;
}

export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  user: UserProfile;
}

export interface ApiErrorShape {
  code: string;
  message: string;
  details?: Record<string, string[]>;
}
