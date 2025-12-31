import React, { useState } from 'react';
import {
    Box,
    Button,
    Paper,
    Typography,
    List,
    ListItem,
    ListItemText,
    IconButton,
    LinearProgress,
    Alert,
    Chip
} from '@mui/material';
import { CloudUpload, Delete, CheckCircle } from '@mui/icons-material';
import axios from 'axios';
import { UploadResponse } from '../types';

interface FileUploadProps {
    onFilesUploaded: (filePaths: string[]) => void;
    sx?: any;
}

const FileUpload: React.FC<FileUploadProps> = ({ onFilesUploaded, sx }) => {
    const [files, setFiles] = useState<File[]>([]);
    const [uploadedPaths, setUploadedPaths] = useState<string[]>([]);
    const [uploading, setUploading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        if (event.target.files) {
            const newFiles = Array.from(event.target.files).filter(file =>
                file.name.toLowerCase().endsWith('.dll')
            );
            setFiles(prev => [...prev, ...newFiles]);
            setError(null);
        }
    };

    const removeFile = (index: number) => {
        setFiles(prev => prev.filter((_, i) => i !== index));
    };

    const handleUpload = async () => {
        if (files.length === 0) {
            setError('Please select at least one DLL file');
            return;
        }

        setUploading(true);
        setError(null);

        const paths: string[] = [];
        const uploadPromises = files.map(async (file) => {
            const formData = new FormData();
            formData.append('File', file);

            try {
                const response = await axios.post<UploadResponse>('http://localhost:5221/api/tests/upload', formData, {
                    headers: {
                        'Content-Type': 'multipart/form-data',
                    },
                });
                paths.push(response.data.filePath);
                return { success: true, file: file.name };
            } catch (err: any) {
                return {
                    success: false,
                    file: file.name,
                    error: err.response?.data?.error || err.message
                };
            }
        });

        const results = await Promise.all(uploadPromises);
        const failedUploads = results.filter(r => !r.success);

        if (failedUploads.length > 0) {
            setError(`Failed to upload: ${failedUploads.map(f => `${f.file} (${f.error})`).join(', ')}`);
        }

        if (paths.length > 0) {
            setUploadedPaths(paths);
            onFilesUploaded(paths);
        }

        setUploading(false);
    };

    return (
        <Paper sx={{ p: 3, ...sx }}>
            <Typography variant="h6" gutterBottom>
                Upload Test Assemblies
            </Typography>

            <Box sx={{ mb: 2 }}>
                <input
                    accept=".dll"
                    style={{ display: 'none' }}
                    id="file-upload"
                    type="file"
                    multiple
                    onChange={handleFileChange}
                />
                <label htmlFor="file-upload">
                    <Button
                        variant="contained"
                        component="span"
                        startIcon={<CloudUpload />}
                        fullWidth
                    >
                        Select DLL Files
                    </Button>
                </label>
                <Typography variant="caption" color="text.secondary" sx={{ mt: 1, display: 'block' }}>
                    Select one or more .dll files containing tests
                </Typography>
            </Box>

            {files.length > 0 && (
                <Box sx={{ mt: 2 }}>
                    <Typography variant="subtitle1" gutterBottom>
                        Selected Files ({files.length})
                    </Typography>
                    <List dense>
                        {files.map((file, index) => (
                            <ListItem
                                key={index}
                                secondaryAction={
                                    <IconButton edge="end" onClick={() => removeFile(index)}>
                                        <Delete />
                                    </IconButton>
                                }
                            >
                                <ListItemText
                                    primary={file.name}
                                    secondary={`${(file.size / 1024).toFixed(2)} KB`}
                                />
                            </ListItem>
                        ))}
                    </List>
                </Box>
            )}

            {uploadedPaths.length > 0 && (
                <Box sx={{ mt: 2 }}>
                    <Typography variant="subtitle1" gutterBottom>
                        Uploaded Assemblies
                    </Typography>
                    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                        {uploadedPaths.map((path, index) => {
                            const fileName = path.split('\\').pop()?.split('/').pop();
                            return (
                                <Chip
                                    key={index}
                                    icon={<CheckCircle />}
                                    label={fileName}
                                    color="success"
                                    variant="outlined"
                                />
                            );
                        })}
                    </Box>
                </Box>
            )}

            {error && (
                <Alert severity="error" sx={{ mt: 2 }}>
                    {error}
                </Alert>
            )}

            <Box sx={{ mt: 2 }}>
                <Button
                    variant="contained"
                    color="primary"
                    onClick={handleUpload}
                    disabled={uploading || files.length === 0}
                    fullWidth
                >
                    {uploading ? 'Uploading...' : 'Upload Files'}
                </Button>
            </Box>

            {uploading && (
                <LinearProgress sx={{ mt: 2 }} />
            )}
        </Paper>
    );
};

export default FileUpload;