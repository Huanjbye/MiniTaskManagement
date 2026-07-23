"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { api } from "@/lib/api";

export default function LoginPage() {
    const router = useRouter();

    const [email, setEmail] = useState("");
    const [password, setPassword] =
        useState("");
    const [loading, setLoading] =
        useState(false);

    const handleLogin = async () => {
        try {
            if (!email || !password) {
                alert(
                    "Please enter email and password"
                );
                return;
            }

            setLoading(true);

            const res = await api.post(
                "/api/Auth/login",
                {
                    email,
                    password,
                }
            );

            const { token, role, fullName, email: userEmail } =
                res.data;

            localStorage.setItem(
                "token",
                token
            );

            localStorage.setItem(
                "role",
                role
            );

            localStorage.setItem(
                "userName",
                fullName || userEmail || email
            );

            localStorage.setItem(
                "userEmail",
                userEmail || email
            );

            router.push("/dashboard");
        } catch (err: unknown) {
            const error = err as {
                response?: { data?: string };
            };

            alert(
                error.response?.data ||
                    "Login failed"
            );
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="min-h-screen bg-gradient-to-br from-slate-100 to-blue-100 flex items-center justify-center p-4">

            <div className="w-full max-w-md bg-white rounded-3xl shadow-xl border border-slate-200 p-8">

                {/* Logo */}

                <div className="text-center mb-8">

                    <h1 className="text-4xl font-bold text-blue-600">
                        TaskFlow
                    </h1>

                    <p className="text-slate-500 mt-2">
                        Task Management System
                    </p>

                </div>

                {/* Welcome */}

                <div className="mb-8 text-center">

                    <h2 className="text-2xl font-bold text-slate-800">
                        Welcome Back
                    </h2>

                    <p className="text-slate-500 mt-2">
                        Sign in to continue
                    </p>

                </div>

                {/* Form */}

                <div className="space-y-5">

                    <div>

                        <label className="block text-sm font-medium text-slate-700 mb-2">
                            Email
                        </label>

                        <input
                            type="email"
                            value={email}
                            placeholder="Enter your email"
                            onChange={(e) =>
                                setEmail(
                                    e.target.value
                                )
                            }
                            onKeyDown={(e) =>
                                e.key === "Enter" &&
                                handleLogin()
                            }
                            className="w-full border border-slate-300 rounded-xl px-4 py-3 focus:outline-none focus:ring-2 focus:ring-blue-500"
                        />

                    </div>

                    <div>

                        <label className="block text-sm font-medium text-slate-700 mb-2">
                            Password
                        </label>

                        <input
                            type="password"
                            value={password}
                            placeholder="Enter your password"
                            onChange={(e) =>
                                setPassword(
                                    e.target.value
                                )
                            }
                            onKeyDown={(e) =>
                                e.key === "Enter" &&
                                handleLogin()
                            }
                            className="w-full border border-slate-300 rounded-xl px-4 py-3 focus:outline-none focus:ring-2 focus:ring-blue-500"
                        />

                    </div>

                    <button
                        onClick={
                            handleLogin
                        }
                        disabled={loading}
                        className="w-full bg-blue-600 hover:bg-blue-700 text-white py-3 rounded-xl font-semibold transition disabled:opacity-50"
                    >
                        {loading
                            ? "Signing In..."
                            : "Login"}
                    </button>

                </div>

                {/* Footer */}

                <div className="mt-6 text-center text-slate-600">

                    Don&apos;t have an account?

                    <Link
                        href="/register"
                        className="text-blue-600 font-semibold ml-2 hover:underline"
                    >
                        Register
                    </Link>

                </div>

            </div>

        </div>
    );
}
