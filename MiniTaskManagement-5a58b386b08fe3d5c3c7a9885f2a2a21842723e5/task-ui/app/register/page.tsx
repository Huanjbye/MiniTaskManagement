"use client";

import { useState } from "react";
import { api } from "@/lib/api";
import Link from "next/link";
import { useRouter } from "next/navigation";

export default function RegisterPage() {
    const router = useRouter();

    const [fullName, setFullName] =
        useState("");

    const [email, setEmail] =
        useState("");

    const [password, setPassword] =
        useState("");

    const [
        confirmPassword,
        setConfirmPassword,
    ] = useState("");

    const [loading, setLoading] =
        useState(false);

    const register = async () => {
        try {
            if (
                !fullName ||
                !email ||
                !password
            ) {
                alert(
                    "Please fill all fields"
                );
                return;
            }

            if (
                password !==
                confirmPassword
            ) {
                alert(
                    "Passwords do not match"
                );
                return;
            }

            setLoading(true);

            await api.post(
                "/api/auth/register",
                {
                    fullName,
                    email,
                    password,
                }
            );

            alert(
                "Register successful"
            );

            router.push("/login");
        } catch (err) {
            console.log(err);
            alert(
                "Register failed"
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

                {/* Header */}

                <div className="text-center mb-8">

                    <h2 className="text-2xl font-bold text-slate-800">
                        Create Account
                    </h2>

                    <p className="text-slate-500 mt-2">
                        Register to start managing your tasks
                    </p>

                </div>

                {/* Form */}

                <div className="space-y-5">

                    <div>

                        <label className="block text-sm font-medium text-slate-700 mb-2">
                            Full Name
                        </label>

                        <input
                            type="text"
                            value={fullName}
                            placeholder="Enter your full name"
                            onChange={(e) =>
                                setFullName(
                                    e.target.value
                                )
                            }
                            onKeyDown={(e) =>
                                e.key ===
                                    "Enter" &&
                                register()
                            }
                            className="w-full border border-slate-300 rounded-xl px-4 py-3 focus:outline-none focus:ring-2 focus:ring-blue-500"
                        />

                    </div>

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
                                e.key ===
                                    "Enter" &&
                                register()
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
                            placeholder="Enter password"
                            onChange={(e) =>
                                setPassword(
                                    e.target.value
                                )
                            }
                            onKeyDown={(e) =>
                                e.key ===
                                    "Enter" &&
                                register()
                            }
                            className="w-full border border-slate-300 rounded-xl px-4 py-3 focus:outline-none focus:ring-2 focus:ring-blue-500"
                        />

                    </div>

                    <div>

                        <label className="block text-sm font-medium text-slate-700 mb-2">
                            Confirm Password
                        </label>

                        <input
                            type="password"
                            value={
                                confirmPassword
                            }
                            placeholder="Confirm password"
                            onChange={(e) =>
                                setConfirmPassword(
                                    e.target.value
                                )
                            }
                            onKeyDown={(e) =>
                                e.key ===
                                    "Enter" &&
                                register()
                            }
                            className="w-full border border-slate-300 rounded-xl px-4 py-3 focus:outline-none focus:ring-2 focus:ring-blue-500"
                        />

                    </div>

                    <button
                        onClick={register}
                        disabled={loading}
                        className="w-full bg-blue-600 hover:bg-blue-700 text-white py-3 rounded-xl font-semibold transition disabled:opacity-50"
                    >
                        {loading
                            ? "Creating Account..."
                            : "Register"}
                    </button>

                </div>

                {/* Footer */}

                <div className="mt-6 text-center text-slate-600">

                    Already have an account?

                    <Link
                        href="/login"
                        className="text-blue-600 font-semibold ml-2 hover:underline"
                    >
                        Sign In
                    </Link>

                </div>

            </div>

        </div>
    );
}