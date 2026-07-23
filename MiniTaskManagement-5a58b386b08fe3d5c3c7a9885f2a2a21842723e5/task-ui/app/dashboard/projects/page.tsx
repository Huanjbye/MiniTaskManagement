"use client";

import { useEffect, useState } from "react";
import { api } from "@/lib/api";

type Project = {
    id: string;
    name: string;
    description?: string | null;
    status: string;
    startDate?: string | null;
    dueDate?: string | null;
    taskCount: number;
    doneTaskCount: number;
    progress: number;
};

const toDateText = (value?: string | null) =>
    value ? new Date(value).toLocaleDateString("vi-VN") : "-";

export default function ProjectsPage() {
    const [projects, setProjects] = useState<Project[]>([]);
    const [name, setName] = useState("");
    const [description, setDescription] = useState("");
    const [startDate, setStartDate] = useState("");
    const [dueDate, setDueDate] = useState("");
    const [loading, setLoading] = useState(false);

    const fetchProjects = async () => {
        const res = await api.get<Project[]>("/api/projects");

        return res.data;
    };

    const refreshProjects = async () => {
        const data = await fetchProjects();

        setProjects(data);
    };

    useEffect(() => {
        let ignore = false;

        const load = async () => {
            try {
                const data = await fetchProjects();

                if (!ignore) {
                    setProjects(data);
                }
            } catch (err) {
                console.log(err);
            }
        };

        void load();

        return () => {
            ignore = true;
        };
    }, []);

    const createProject = async () => {
        if (!name.trim()) {
            alert("Vui lòng nhập tên project");
            return;
        }

        try {
            setLoading(true);
            await api.post("/api/projects", {
                name,
                description,
                startDate: startDate || null,
                dueDate: dueDate || null,
            });

            setName("");
            setDescription("");
            setStartDate("");
            setDueDate("");
            await refreshProjects();
        } catch (err) {
            console.log(err);
            alert("Tạo project thất bại");
        } finally {
            setLoading(false);
        }
    };

    const deleteProject = async (id: string) => {
        if (!confirm("Xóa project này?")) {
            return;
        }

        try {
            await api.delete(`/api/projects/${id}`);
            await refreshProjects();
        } catch (err) {
            console.log(err);
            alert("Xóa project thất bại");
        }
    };

    return (
        <div className="space-y-5">
            <div>
                <h1 className="text-2xl font-bold text-slate-800">
                    Project
                </h1>
                <p className="mt-1 text-sm text-slate-500">
                    Nhóm nhiều công việc vào cùng một project và theo dõi
                    tiến độ.
                </p>
            </div>

            <section className="rounded-md border border-slate-200 bg-white p-4 shadow-sm">
                <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-[1fr_1.5fr_180px_180px_auto]">
                    <input
                        value={name}
                        onChange={(event) =>
                            setName(event.target.value)
                        }
                        placeholder="Tên project"
                        className="h-10 rounded-md border border-slate-200 px-3 text-sm outline-none focus:border-[#16b99f]"
                    />
                    <input
                        value={description}
                        onChange={(event) =>
                            setDescription(event.target.value)
                        }
                        placeholder="Mô tả"
                        className="h-10 rounded-md border border-slate-200 px-3 text-sm outline-none focus:border-[#16b99f]"
                    />
                    <input
                        type="date"
                        value={startDate}
                        onChange={(event) =>
                            setStartDate(event.target.value)
                        }
                        className="h-10 rounded-md border border-slate-200 px-3 text-sm"
                    />
                    <input
                        type="date"
                        value={dueDate}
                        onChange={(event) =>
                            setDueDate(event.target.value)
                        }
                        className="h-10 rounded-md border border-slate-200 px-3 text-sm"
                    />
                    <button
                        onClick={createProject}
                        disabled={loading}
                        className="h-10 rounded-md bg-[#16b99f] px-4 text-sm font-bold text-white hover:bg-[#119f89] disabled:opacity-50"
                    >
                        Tạo project
                    </button>
                </div>
            </section>

            <section className="grid gap-4 xl:grid-cols-2">
                {projects.length === 0 ? (
                    <div className="rounded-md border border-slate-200 bg-white p-8 text-center text-slate-500">
                        Chưa có project nào
                    </div>
                ) : (
                    projects.map((project) => (
                        <article
                            key={project.id}
                            className="rounded-md border border-slate-200 bg-white p-5 shadow-sm"
                        >
                            <div className="flex items-start justify-between gap-3">
                                <div>
                                    <h2 className="text-lg font-bold text-slate-800">
                                        {project.name}
                                    </h2>
                                    {project.description && (
                                        <p className="mt-1 text-sm text-slate-500">
                                            {project.description}
                                        </p>
                                    )}
                                </div>
                                <span className="rounded-full bg-emerald-100 px-3 py-1 text-xs font-bold text-emerald-700">
                                    {project.status}
                                </span>
                            </div>

                            <div className="mt-4 grid gap-3 text-sm text-slate-600 sm:grid-cols-3">
                                <span>
                                    Bắt đầu:{" "}
                                    {toDateText(project.startDate)}
                                </span>
                                <span>
                                    Hạn: {toDateText(project.dueDate)}
                                </span>
                                <span>
                                    Task: {project.doneTaskCount}/
                                    {project.taskCount}
                                </span>
                            </div>

                            <div className="mt-4">
                                <div className="mb-1 flex justify-between text-xs font-semibold text-slate-500">
                                    <span>Tiến độ</span>
                                    <span>{project.progress}%</span>
                                </div>
                                <div className="h-3 overflow-hidden rounded-full bg-slate-100">
                                    <div
                                        className="h-full rounded-full bg-[#16b99f]"
                                        style={{
                                            width: `${project.progress}%`,
                                        }}
                                    />
                                </div>
                            </div>

                            <div className="mt-4 flex justify-end">
                                <button
                                    onClick={() =>
                                        deleteProject(project.id)
                                    }
                                    className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-xs font-bold text-red-700 hover:bg-red-100"
                                >
                                    Xóa
                                </button>
                            </div>
                        </article>
                    ))
                )}
            </section>
        </div>
    );
}
