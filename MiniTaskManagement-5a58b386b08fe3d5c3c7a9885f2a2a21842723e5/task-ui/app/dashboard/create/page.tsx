"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { api } from "@/lib/api";

type ProjectOption = {
    id: string;
    name: string;
};

const toNumberOrNull = (value: string) => {
    if (!value.trim()) {
        return null;
    }

    return Number(value);
};

const splitTags = (value: string) =>
    value
        .split(",")
        .map((tag) => tag.trim())
        .filter(Boolean);

export default function CreateTask() {
    const router = useRouter();

    const [projects, setProjects] = useState<ProjectOption[]>([]);

    const [title, setTitle] = useState("");
    const [description, setDescription] = useState("");
    const [priority, setPriority] = useState("Medium");
    const [projectId, setProjectId] = useState("");
    const [dueDate, setDueDate] = useState("");
    const [reminderAt, setReminderAt] = useState("");
    const [estimatedMinutes, setEstimatedMinutes] =
        useState("");
    const [actualMinutes, setActualMinutes] = useState("");
    const [tagInput, setTagInput] = useState("");
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        const loadProjects = async () => {
            try {
                const res = await api.get<ProjectOption[]>(
                    "/api/projects"
                );

                setProjects(res.data);
            } catch (err) {
                console.log(err);
            }
        };

        void loadProjects();
    }, []);

    const createTask = async () => {
        try {
            if (!title.trim()) {
                alert("Vui lòng nhập tiêu đề công việc");
                return;
            }

            setLoading(true);

            await api.post("/api/tasks", {
                title,
                description,
                priority,
                projectId: projectId || null,
                dueDate: dueDate || new Date().toISOString(),
                reminderAt: reminderAt || null,
                estimatedMinutes:
                    toNumberOrNull(estimatedMinutes),
                actualMinutes:
                    toNumberOrNull(actualMinutes) ?? 0,
                tagNames: splitTags(tagInput),
            });

            router.push("/dashboard");
        } catch (err) {
            console.log(err);
            alert("Tạo công việc thất bại");
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="mx-auto max-w-5xl">
            <div className="rounded-md border border-slate-200 bg-white p-6 shadow-sm">
                <div className="mb-6">
                    <h1 className="text-2xl font-bold text-slate-800">
                        Tạo công việc
                    </h1>
                    <p className="mt-1 text-sm text-slate-500">
                        Thêm thông tin nâng cao như project, tag,
                        thời gian ước tính và nhắc hạn.
                    </p>
                </div>

                <div className="space-y-5">
                    <div>
                        <label className="mb-2 block text-sm font-semibold text-slate-700">
                            Tiêu đề
                        </label>
                        <input
                            type="text"
                            value={title}
                            placeholder="Nhập tiêu đề công việc"
                            onChange={(event) =>
                                setTitle(event.target.value)
                            }
                            className="w-full rounded-md border border-slate-300 px-4 py-3 outline-none focus:border-[#16b99f]"
                        />
                    </div>

                    <div>
                        <label className="mb-2 block text-sm font-semibold text-slate-700">
                            Mô tả
                        </label>
                        <textarea
                            rows={5}
                            value={description}
                            placeholder="Nhập mô tả công việc"
                            onChange={(event) =>
                                setDescription(event.target.value)
                            }
                            className="w-full resize-none rounded-md border border-slate-300 px-4 py-3 outline-none focus:border-[#16b99f]"
                        />
                    </div>

                    <div className="grid gap-5 md:grid-cols-2">
                        <div>
                            <label className="mb-2 block text-sm font-semibold text-slate-700">
                                Project
                            </label>
                            <select
                                value={projectId}
                                onChange={(event) =>
                                    setProjectId(
                                        event.target.value
                                    )
                                }
                                className="w-full rounded-md border border-slate-300 px-4 py-3"
                            >
                                <option value="">
                                    Không gắn project
                                </option>
                                {projects.map((project) => (
                                    <option
                                        key={project.id}
                                        value={project.id}
                                    >
                                        {project.name}
                                    </option>
                                ))}
                            </select>
                        </div>

                        <div>
                            <label className="mb-2 block text-sm font-semibold text-slate-700">
                                Độ ưu tiên
                            </label>
                            <select
                                value={priority}
                                onChange={(event) =>
                                    setPriority(
                                        event.target.value
                                    )
                                }
                                className="w-full rounded-md border border-slate-300 px-4 py-3"
                            >
                                <option value="Low">Thấp</option>
                                <option value="Medium">
                                    Trung bình
                                </option>
                                <option value="High">Cao</option>
                                <option value="Critical">
                                    Khẩn cấp
                                </option>
                            </select>
                        </div>
                    </div>

                    <div className="grid gap-5 md:grid-cols-2">
                        <div>
                            <label className="mb-2 block text-sm font-semibold text-slate-700">
                                Hạn xử lý
                            </label>
                            <input
                                type="date"
                                value={dueDate}
                                min={
                                    new Date()
                                        .toISOString()
                                        .split("T")[0]
                                }
                                onChange={(event) =>
                                    setDueDate(event.target.value)
                                }
                                className="w-full rounded-md border border-slate-300 px-4 py-3"
                            />
                        </div>

                        <div>
                            <label className="mb-2 block text-sm font-semibold text-slate-700">
                                Nhắc trước hạn
                            </label>
                            <input
                                type="datetime-local"
                                value={reminderAt}
                                onChange={(event) =>
                                    setReminderAt(
                                        event.target.value
                                    )
                                }
                                className="w-full rounded-md border border-slate-300 px-4 py-3"
                            />
                        </div>
                    </div>

                    <div className="grid gap-5 md:grid-cols-2">
                        <div>
                            <label className="mb-2 block text-sm font-semibold text-slate-700">
                                Thời gian ước tính (phút)
                            </label>
                            <input
                                type="number"
                                min="0"
                                value={estimatedMinutes}
                                onChange={(event) =>
                                    setEstimatedMinutes(
                                        event.target.value
                                    )
                                }
                                className="w-full rounded-md border border-slate-300 px-4 py-3"
                            />
                        </div>

                        <div>
                            <label className="mb-2 block text-sm font-semibold text-slate-700">
                                Thời gian thực tế (phút)
                            </label>
                            <input
                                type="number"
                                min="0"
                                value={actualMinutes}
                                onChange={(event) =>
                                    setActualMinutes(
                                        event.target.value
                                    )
                                }
                                className="w-full rounded-md border border-slate-300 px-4 py-3"
                            />
                        </div>
                    </div>

                    <div>
                        <label className="mb-2 block text-sm font-semibold text-slate-700">
                            Tag
                        </label>
                        <input
                            type="text"
                            value={tagInput}
                            placeholder="Ví dụ: Backend, Bug, Feature"
                            onChange={(event) =>
                                setTagInput(event.target.value)
                            }
                            className="w-full rounded-md border border-slate-300 px-4 py-3 outline-none focus:border-[#16b99f]"
                        />
                        <p className="mt-1 text-xs text-slate-500">
                            Phân tách nhiều tag bằng dấu phẩy.
                        </p>
                    </div>

                    <div className="flex justify-end gap-3 pt-3">
                        <button
                            onClick={() => router.push("/dashboard")}
                            className="rounded-md border border-slate-300 px-5 py-3 font-semibold hover:bg-slate-50"
                        >
                            Hủy
                        </button>
                        <button
                            onClick={createTask}
                            disabled={loading}
                            className="rounded-md bg-[#16b99f] px-5 py-3 font-bold text-white hover:bg-[#119f89] disabled:opacity-50"
                        >
                            {loading
                                ? "Đang tạo..."
                                : "Tạo công việc"}
                        </button>
                    </div>
                </div>
            </div>
        </div>
    );
}
